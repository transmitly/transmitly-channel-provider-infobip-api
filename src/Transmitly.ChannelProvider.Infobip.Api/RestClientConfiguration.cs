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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Transmitly.ChannelProvider.Infobip.Configuration;
using Transmitly.Util;

namespace Transmitly.ChannelProvider.Infobip.Api
{
	internal static class RestClientConfiguration
	{
		// Dispatchers can be created per dispatch; one handler (connection pool) per proxy keeps sockets from leaking.
		private static readonly ConcurrentDictionary<IWebProxy, HttpMessageHandler> _proxyHandlers = new();

		public static HttpClient? CreateHttpClient(InfobipChannelProviderConfiguration? channelProviderConfiguration)
		{
			if (channelProviderConfiguration?.WebProxy == null)
				return null;

			var handler = _proxyHandlers.GetOrAdd(channelProviderConfiguration.WebProxy, proxy => new HttpClientHandler
			{
				Proxy = proxy,
				UseCookies = false
			});
			return new HttpClient(handler, disposeHandler: false);
		}

		public static void Configure(HttpClient httpClient, InfobipChannelProviderConfiguration channelProviderConfiguration)
		{
			Guard.AgainstNull(httpClient);
			Guard.AgainstNull(channelProviderConfiguration);

			httpClient.BaseAddress = new Uri(Guard.AgainstNullOrWhiteSpace(channelProviderConfiguration.BasePath));
			httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(channelProviderConfiguration.UserAgent);
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(channelProviderConfiguration.ApiKeyPrefix, channelProviderConfiguration.ApiKey);
		}
	}
}