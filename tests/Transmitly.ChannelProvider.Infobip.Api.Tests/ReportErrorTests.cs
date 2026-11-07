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
using Transmitly.ChannelProvider.Infobip.Api.Sms;
using Transmitly.ChannelProvider.Infobip.Api.Voice;
using Transmitly.ChannelProvider.Infobip.Configuration;
using Transmitly.Delivery;

namespace Transmitly.ChannelProvider.Infobip.Api.Tests
{
	/// <summary>
	/// Infobip sends an error object on every delivery report; only a non-zero error id is a failure,
	/// </summary>
	[TestClass]
	public sealed class ReportErrorTests
	{
		private const string NoError = """{"groupId":0,"groupName":"OK","id":0,"name":"NO_ERROR","description":"No Error","permanent":false}""";
		private const string AbsentSubscriber = """{"groupId":1,"groupName":"HANDSET_ERRORS","id":27,"name":"EC_ABSENT_SUBSCRIBER","description":"Absent Subscriber","permanent":false}""";

		[TestMethod]
		[DataRow("sms")]
		[DataRow("voice")]
		[DataRow("email")]
		public async Task NoError_IsASuccessWithoutDetail(string channel)
		{
			var report = await AdaptAsync(channel, """{"groupId":3,"groupName":"DELIVERED"}""", NoError);

			Assert.IsTrue(report.Status.IsSuccess());
			Assert.IsNull(report.Status.Detail);
			Assert.IsNull(report.Exception);
		}

		[TestMethod]
		[DataRow("sms")]
		[DataRow("voice")]
		[DataRow("email")]
		public async Task RealError_IsAFailureWithInfobipsDescription(string channel)
		{
			var report = await AdaptAsync(channel, """{"groupId":2,"groupName":"UNDELIVERABLE"}""", AbsentSubscriber);

			Assert.IsTrue(report.Status.IsFailure());
			Assert.AreEqual("Absent Subscriber", report.Status.Detail);
			Assert.IsNull(report.Exception);
		}

		private static async Task<DeliveryReport> AdaptAsync(string channel, string status, string error)
		{
			var configuration = new InfobipChannelProviderConfiguration { ValidateDeliveryReportSignature = false };
			IChannelProviderDeliveryReportRequestAdaptor adaptor = channel switch
			{
				"sms" => new SmsDeliveryStatusReportAdaptor(configuration),
				"voice" => new VoiceDeliveryStatusReportAdaptor(configuration),
				_ => new EmailDeliveryStatusReportAdaptor(configuration)
			};
			var body = $$"""{"results":[{"messageId":"m1","status":{{status}},"error":{{error}}}]}""";

			return (await adaptor.AdaptAsync(new Request(channel, body)))!.Single();
		}

		private sealed class Request(string channel, string body) : IRequestAdaptorContext
		{
			public string? GetQueryValue(string key) => key == DeliveryUtil.ChannelIdKey ? channel : key == DeliveryUtil.ChannelProviderIdKey ? Id.ChannelProvider.Infobip() : null;
			public string? GetFormValue(string key) => null;
			public string? GetHeaderValue(string key) => null;
			public string? GetValue(string key) => GetQueryValue(key);
			public string? Content => body;
			public string? RequestUrl => null;
			public string? PipelineIntent => null;
			public string? PipelineId => null;
			public string? ResourceId => null;
		}
	}
}
