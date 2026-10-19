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

using System.Net;
using Transmitly.ChannelProvider.Infobip.Api.Email;
using Transmitly.ChannelProvider.Infobip.Api.Sms;
using Transmitly.ChannelProvider.Infobip.Api.Voice;
using Transmitly.ChannelProvider.Infobip.Configuration;

namespace Transmitly.ChannelProvider.Infobip.Api.Tests
{
	[TestClass]
	public sealed class HttpConnectionTests
	{
		private const int DispatchCount = 5;
		// .invalid never resolves, so a request that skips the proxy cannot leave the machine.
		private const string ProxiedTarget = "http://api.infobip.invalid/";

		[TestMethod]
		[DataRow(typeof(EmailChannelProviderDispatcher))]
		[DataRow(typeof(SmsChannelProviderDispatcher))]
		[DataRow(typeof(VoiceChannelProviderDispatcher))]
		public async Task ConfiguredWebProxyShouldBeUsed(Type dispatcherType)
		{
			using var proxy = new LoopbackHttpServer();
			var options = new InfobipChannelProviderConfiguration { ApiKey = "test-key", WebProxy = new WebProxy(proxy.Url) };

			await LoopbackHttpServer.SendWithDispatcherClientAsync(CreateDispatcher(dispatcherType, options), ProxiedTarget);

			Assert.AreEqual(1, proxy.RequestCount, "The request did not go through the configured WebProxy.");
		}

		[TestMethod]
		[DataRow(typeof(EmailChannelProviderDispatcher))]
		[DataRow(typeof(SmsChannelProviderDispatcher))]
		[DataRow(typeof(VoiceChannelProviderDispatcher))]
		public async Task ProxiedDispatchersShouldShareOneConnection(Type dispatcherType)
		{
			using var proxy = new LoopbackHttpServer();
			var options = new InfobipChannelProviderConfiguration { ApiKey = "test-key", WebProxy = new WebProxy(proxy.Url) };

			for (var i = 0; i < DispatchCount; i++)
				await LoopbackHttpServer.SendWithDispatcherClientAsync(CreateDispatcher(dispatcherType, options), ProxiedTarget);

			Assert.AreEqual(DispatchCount, proxy.RequestCount, "Precondition: every dispatch went through the configured WebProxy.");
			Assert.AreEqual(1, proxy.ConnectionCount, "Each dispatcher opened its own connection to the proxy.");
		}

		[TestMethod]
		[DataRow(typeof(EmailChannelProviderDispatcher))]
		[DataRow(typeof(SmsChannelProviderDispatcher))]
		[DataRow(typeof(VoiceChannelProviderDispatcher))]
		public async Task DispatchersWithoutProxyShouldShareOneConnection(Type dispatcherType)
		{
			using var server = new LoopbackHttpServer();
			var options = new InfobipChannelProviderConfiguration { ApiKey = "test-key" };

			for (var i = 0; i < DispatchCount; i++)
				await LoopbackHttpServer.SendWithDispatcherClientAsync(CreateDispatcher(dispatcherType, options), server.Url);

			Assert.AreEqual(DispatchCount, server.RequestCount, "Precondition: every dispatch reached the server.");
			Assert.AreEqual(1, server.ConnectionCount, "Each dispatcher opened its own connection; requires the Transmitly core fix.");
		}

		private static object CreateDispatcher(Type dispatcherType, InfobipChannelProviderConfiguration options) =>
			Activator.CreateInstance(dispatcherType, options)!;
	}
}
