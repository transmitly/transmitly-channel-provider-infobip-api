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
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Transmitly.ChannelProvider.Infobip.Api.Tests
{
	/// <summary>
	/// Minimal keep-alive HTTP server on a loopback port. Answers 200 to every request (including proxied,
	/// absolute-form requests) and counts the connections it accepts and the requests it receives.
	/// </summary>
	internal sealed class LoopbackHttpServer : IDisposable
	{
		private static readonly byte[] OkResponse = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n");
		private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
		private int _connectionCount;
		private int _requestCount;

		public LoopbackHttpServer()
		{
			_listener.Start();
			Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/";
			_ = AcceptConnectionsAsync();
		}

		public string Url { get; }

		public int ConnectionCount => Volatile.Read(ref _connectionCount);

		public int RequestCount => Volatile.Read(ref _requestCount);

		public void Dispose() => _listener.Stop();

		/// <summary>
		/// Sends a GET with the HttpClient the dispatcher dispatches with. ChannelProviderRestDispatcher only exposes it
		/// to subclasses, so it is read through reflection. Request failures are swallowed so callers can assert on what
		/// the server observed.
		/// </summary>
		public static async Task SendWithDispatcherClientAsync(object dispatcher, string url)
		{
			var client = (HttpClient)dispatcher.GetType()
				.GetProperty("HttpClient", BindingFlags.Instance | BindingFlags.NonPublic)!
				.GetValue(dispatcher)!;

			try
			{
				using var response = await client.GetAsync(url).ConfigureAwait(false);
			}
			catch (HttpRequestException)
			{
				// the request never reached the server; the caller's assertions report it
			}
		}

		private async Task AcceptConnectionsAsync()
		{
			try
			{
				while (true)
				{
					var connection = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
					Interlocked.Increment(ref _connectionCount);
					_ = RespondToEveryRequestAsync(connection);
				}
			}
			catch (Exception)
			{
				// listener stopped
			}
		}

		private async Task RespondToEveryRequestAsync(TcpClient connection)
		{
			using (connection)
			{
				var stream = connection.GetStream();
				var buffer = new byte[4096];
				var pending = string.Empty;
				try
				{
					int read;
					while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
					{
						pending += Encoding.ASCII.GetString(buffer, 0, read);
						int end;
						while ((end = pending.IndexOf("\r\n\r\n", StringComparison.Ordinal)) >= 0)
						{
							pending = pending.Substring(end + 4);
							Interlocked.Increment(ref _requestCount);
							await stream.WriteAsync(OkResponse, 0, OkResponse.Length).ConfigureAwait(false);
						}
					}
				}
				catch (Exception)
				{
					// client closed the connection
				}
			}
		}
	}
}
