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

using Transmitly.ChannelProvider.Infobip.Api.Email;
using Transmitly.ChannelProvider.Infobip.Configuration;
using Transmitly.Delivery;

namespace Transmitly.ChannelProvider.Infobip.Api.Tests
{
	[TestClass]
	public sealed class EmailDeliveryReportTests
	{
		// Fields from Infobip's EmailWebhookDeliveryReport (their generated C# client); Infobip reports an error object even on success.
		private const string Payload = """
			{"results":[{"bulkId":"bulk-1","messageId":"msg-1","to":"rider@example.com",
			"sentAt":"2026-11-06T10:00:00.000+0000","doneAt":"2026-11-06T10:00:05.000+0000","smsCount":1,"callbackData":"order-42",
			"price":{"pricePerMessage":0.0012,"currency":"EUR"},
			"status":{"groupId":3,"groupName":"DELIVERED","id":5,"name":"DELIVERED_TO_HANDSET","description":"Message delivered to handset"},
			"error":{"groupId":0,"groupName":"OK","id":0,"name":"NO_ERROR","description":"No Error","permanent":false},
			"browserLink":"https://view.example.com/msg-1","sendingIp":"192.0.2.10"}]}
			""";

		[TestMethod]
		public async Task EmailWebhook_ValuesAreReadableFromTheReport()
		{
			var adaptor = new EmailDeliveryStatusReportAdaptor(new InfobipChannelProviderConfiguration { ValidateDeliveryReportSignature = false });

			var report = (await adaptor.AdaptAsync(new EmailRequest()))!.Single();

			// Nothing is registered: the values come straight from the report.
			var email = report.Infobip().Email;
			Assert.AreEqual("msg-1", report.ResourceId);
			Assert.AreEqual("bulk-1", email.BulkId);
			Assert.AreEqual("msg-1", email.MessageId);
			Assert.AreEqual("rider@example.com", email.To);
			Assert.AreEqual(new DateTimeOffset(2026, 11, 6, 10, 0, 0, TimeSpan.Zero), email.SentAt);
			Assert.AreEqual(new DateTimeOffset(2026, 11, 6, 10, 0, 5, TimeSpan.Zero), email.DoneAt);
			Assert.AreEqual(1, email.SmsCount);
			Assert.AreEqual("order-42", email.CallbackData);
			Assert.AreEqual(0.0012, email.Price!.PricePerMessage);
			Assert.AreEqual("EUR", email.Price.Currency);
			Assert.AreEqual("DELIVERED", email.Status!.GroupName);
			Assert.AreEqual("NO_ERROR", email.Error!.Name);
			Assert.AreEqual("https://view.example.com/msg-1", email.BrowserLink);
			Assert.AreEqual("192.0.2.10", email.SendingIp);
			Assert.IsNull(report.Infobip().Sms.MessageId, "Email values must not appear as SMS values.");
		}

		private sealed class EmailRequest : IRequestAdaptorContext
		{
			public string? GetQueryValue(string key) => key == DeliveryUtil.ChannelIdKey ? Id.Channel.Email() : key == DeliveryUtil.ChannelProviderIdKey ? Id.ChannelProvider.Infobip() : null;
			public string? GetFormValue(string key) => null;
			public string? GetHeaderValue(string key) => null;
			public string? GetValue(string key) => throw new System.NotSupportedException("The ASP.NET hosts throw here; use GetQueryValue, GetFormValue or GetHeaderValue.");
			public string? Content => Payload;
			public string? RequestUrl => null;
			public string? PipelineIntent => null;
			public string? PipelineId => null;
			public string? ResourceId => null;
		}
	}
}
