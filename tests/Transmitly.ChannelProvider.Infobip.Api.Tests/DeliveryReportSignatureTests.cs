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

using System.Security.Cryptography;
using System.Text;
using Transmitly.ChannelProvider.Infobip.Api.Sms;
using Transmitly.ChannelProvider.Infobip.Configuration;
using Transmitly.Delivery;

namespace Transmitly.ChannelProvider.Infobip.Api.Tests
{
	[TestClass]
	public sealed class DeliveryReportSignatureTests
	{
		private const string Secret = "infobip-secret";
		private const string Body = "{\"results\":[{\"messageId\":\"m1\",\"status\":{\"groupId\":3}}]}";

		[TestMethod]
		public async Task AdaptAsync_ValidatesSignature()
		{
			byte[] hash;
			using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret)))
				hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(Body));
			var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			var adaptor = new SmsDeliveryStatusReportAdaptor(new InfobipChannelProviderConfiguration { WebhookSecret = Secret });

			var report = (await adaptor.AdaptAsync(new FakeRequest(hex)))!.Single();
			Assert.AreEqual("m1", report.ResourceId);
			Assert.AreEqual("m1", report.Infobip().Sms.MessageId, "Report values are readable with nothing registered.");
			Assert.IsNotNull(await adaptor.AdaptAsync(new FakeRequest("sha256=" + Convert.ToBase64String(hash))));
			await Assert.ThrowsExactlyAsync<DeliveryReportRequestValidationException>(() => adaptor.AdaptAsync(new FakeRequest(new string('0', 64))));
			await Assert.ThrowsExactlyAsync<DeliveryReportRequestValidationException>(() => adaptor.AdaptAsync(new FakeRequest(null)));
			await Assert.ThrowsExactlyAsync<DeliveryReportRequestValidationException>(() => new SmsDeliveryStatusReportAdaptor(new InfobipChannelProviderConfiguration()).AdaptAsync(new FakeRequest(hex)));
			Assert.IsNotNull(await new SmsDeliveryStatusReportAdaptor(new InfobipChannelProviderConfiguration { ValidateDeliveryReportSignature = false }).AdaptAsync(new FakeRequest(null)));
			Assert.IsNull(await adaptor.AdaptAsync(new FakeRequest(null, channelId: "email")));
		}

		private sealed class FakeRequest(string? signature, string channelId = "sms") : IRequestAdaptorContext
		{
			public string? GetQueryValue(string key) => key == DeliveryUtil.ChannelIdKey ? channelId : key == DeliveryUtil.ChannelProviderIdKey ? Id.ChannelProvider.Infobip() : null;
			public string? GetFormValue(string key) => null;
			public string? GetHeaderValue(string key) => key.Equals("X-Hub-Signature", StringComparison.OrdinalIgnoreCase) ? signature : null;
			public string? GetValue(string key) => throw new System.NotSupportedException("The ASP.NET hosts throw here; use GetQueryValue, GetFormValue or GetHeaderValue.");
			public string? Content => Body;
			public string? RequestUrl => null;
			public string? PipelineIntent => null;
			public string? PipelineId => null;
			public string? ResourceId => null;
		}
	}
}
