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

namespace Transmitly.ChannelProvider.Infobip.Api.Sms
{
	internal static class ExtendedSmsDeliveryReportExtension
	{
		public static SmsDeliveryReport ApplyExtendedProperties(this SmsDeliveryReport voiceDeliveryReport, SmsStatusReport report)
		{
			var sms = voiceDeliveryReport.Infobip().Sms;
			sms.BulkId = report.BulkId;
			sms.MessageId = report.MessageId;
			sms.To = report.To;
			sms.From = report.From;
			sms.MccMnc = report.MccMnc;
			sms.CallbackData = report.CallbackData;
			sms.Price = report.Price;
			sms.ApplicationId = report.ApplicationId;
			sms.EntityId = report.EntityId;
			sms.Status = report.Status;
			sms.Error = report.Error;
			return voiceDeliveryReport;
		}
	}
}