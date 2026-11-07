// Copyright (c) Code Impressions, LLC. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License")
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System.Globalization;
using System.Net;
using System.Text;
using Moq;
using Transmitly.ChannelProvider.Infobip.Api.Email;
using Transmitly.ChannelProvider.Infobip.Configuration;

namespace Transmitly.ChannelProvider.Infobip.Api.Tests
{
	/// <summary>
	/// Checks the multipart form the email dispatcher posts to Infobip.
	/// </summary>
	[TestClass]
	public sealed class EmailDispatcherTests
	{
		[TestMethod]
		public async Task AttachmentAndInlineImage_AreSent()
		{
			var handler = new CapturingHandler();
			var dispatcher = new EmailChannelProviderDispatcher(new InfobipChannelProviderConfiguration { ApiKey = "test", BasePath = "https://infobip.test" }, new HttpClient(handler));
			var email = Email(Attachment("statement.txt", "text/plain", "statement"));
			var context = Context(new LinkedResource("cid:logo", new MemoryStream(Encoding.UTF8.GetBytes("png-bytes"))) { ContentType = "image/png" });

			// Sent twice: each recipient's email reuses the same resource streams.
			await dispatcher.DispatchAsync(email, context, CancellationToken.None);
			await dispatcher.DispatchAsync(email, context, CancellationToken.None);

			var attachment = handler.Parts.Single(p => p.Name == "attachment");
			Assert.AreEqual("statement.txt", attachment.FileName);
			Assert.AreEqual("text/plain", attachment.ContentType);
			Assert.AreEqual("statement", attachment.Body);

			// Infobip matches cid:logo against the inline image's file name.
			var inline = handler.Parts.Single(p => p.Name == "inlineImage");
			Assert.AreEqual("logo", inline.FileName);
			Assert.AreEqual("image/png", inline.ContentType);
			Assert.AreEqual("png-bytes", inline.Body);
		}

		private static IEmail Email(params IEmailAttachment[] attachments)
		{
			var email = new Mock<IEmail>();
			email.SetupGet(e => e.From).Returns("sender@example.com".AsIdentityAddress());
			email.SetupGet(e => e.To).Returns(["rider@example.com".AsIdentityAddress()]);
			email.SetupGet(e => e.Subject).Returns("Your statement");
			email.SetupGet(e => e.HtmlBody).Returns("<img src=\"cid:logo\"> Your statement is attached.");
			email.SetupGet(e => e.Attachments).Returns(attachments);
			email.SetupGet(e => e.ExtendedProperties).Returns(new ExtendedProperties());
			return email.Object;
		}

		private static IEmailAttachment Attachment(string name, string contentType, string content) =>
			Mock.Of<IEmailAttachment>(a => a.Name == name && a.ContentType == contentType && a.ContentStream == new MemoryStream(Encoding.UTF8.GetBytes(content)));

		private static IDispatchCommunicationContext Context(params LinkedResource[] linkedResources)
		{
			var context = new Mock<IDispatchCommunicationContext> { DefaultValue = DefaultValue.Mock };
			context.SetupGet(c => c.CultureInfo).Returns(CultureInfo.InvariantCulture);
			context.SetupGet(c => c.ContentModel).Returns(Mock.Of<IContentModel>(m => m.LinkedResources == linkedResources));
			return context.Object;
		}

		private sealed record Part(string? Name, string? FileName, string? ContentType, string Body);

		private sealed class CapturingHandler : HttpMessageHandler
		{
			public List<Part> Parts { get; } = [];

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				Parts.Clear();
				foreach (var part in (MultipartFormDataContent)request.Content!)
				{
					var disposition = part.Headers.ContentDisposition;
					Parts.Add(new Part(disposition?.Name?.Trim('"'), disposition?.FileName?.Trim('"'), part.Headers.ContentType?.MediaType, await part.ReadAsStringAsync(cancellationToken)));
				}

				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("""{"bulkId":"bulk-1","messages":[{"to":"rider@example.com","messageId":"msg-1","status":{"groupId":1,"groupName":"PENDING","id":26,"name":"PENDING_ACCEPTED"}}]}""")
				};
			}
		}
	}
}
