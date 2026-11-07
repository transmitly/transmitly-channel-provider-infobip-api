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

using System;
using System.Security.Cryptography;
using System.Text;
using Transmitly.ChannelProvider.Infobip.Configuration;
using Transmitly.Delivery;

namespace Transmitly.ChannelProvider.Infobip.Api
{
	// https://www.infobip.com/docs/developer-resources/api-fundamentals/webhooks
	// HMAC-SHA256 of the raw body, sent once an HMAC auth setting is on the notification profile for the URL.
	// todo: add support for other HMAC algorithms as well as Basic and OAuth support.
	internal static class DeliveryReportSignature
	{
		private const string SignatureHeader = "X-Hub-Signature";

		public static void Validate(IRequestAdaptorContext context, InfobipChannelProviderConfiguration configuration)
		{
			if (!configuration.ValidateDeliveryReportSignature)
				return;
			if (string.IsNullOrWhiteSpace(configuration.WebhookSecret))
				throw new DeliveryReportRequestValidationException("Infobip WebhookSecret is required to validate delivery reports.");

			var signature = context.GetHeaderValue(SignatureHeader)?.Trim();
			var prefixEnd = signature?.TrimEnd('=').IndexOf('=') ?? -1; // base64 only has '=' as trailing padding
			if (prefixEnd >= 0)
				signature = signature![(prefixEnd + 1)..];

			using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(configuration.WebhookSecret));
			var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(context.Content ?? string.Empty));
#if NET9_0_OR_GREATER
			var hex = Convert.ToHexStringLower(hash);
#else
			var hex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
#endif

#if !NET
			if (signature == null || !(FixedTimeEquals(hex, signature.ToLowerInvariant()) || FixedTimeEquals(Convert.ToBase64String(hash), signature)))
#else
			var signatureBytes = Encoding.UTF8.GetBytes(signature ?? string.Empty);
			if (signature == null || !(CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hex), signatureBytes) ||
				CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Convert.ToBase64String(hash)), signatureBytes)))
#endif
				throw new DeliveryReportRequestValidationException("Infobip delivery report signature is invalid.");

		}
#if !NET
		private static bool FixedTimeEquals(string expected, string actual)
		{
			if (actual.Length != expected.Length)
				return false;
			var diff = 0;
			for (var i = 0; i < expected.Length; i++)
				diff |= expected[i] ^ actual[i];
			return diff == 0;
		}
#endif
	}
}
