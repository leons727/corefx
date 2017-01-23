﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class WebSocketTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private string _url;

        public WebSocketTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
            _url = _factory.ListeningUrl;
        }

        public void Dispose() => _factory.Dispose();

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task AcceptWebSocketAsync_NullSubProtocol_Succeeds()
        {
            if (PlatformDetection.IsWindows7)
            {
                // Websockets in WinHttp 5.1 is only supported from Windows 8+
                Assert.Throws<PlatformNotSupportedException>(() => new ClientWebSocket());
                return;
            }

            UriBuilder uriBuilder = new UriBuilder(_url);
            uriBuilder.Scheme = "ws";

            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();
            using (ClientWebSocket clientWebSocket = new ClientWebSocket())
            {
                Task clientConnectTask = clientWebSocket.ConnectAsync(uriBuilder.Uri, CancellationToken.None);

                Assert.Equal(WebSocketState.Connecting, clientWebSocket.State);

                HttpListenerContext listenerContext = await serverContextTask;
                HttpListenerWebSocketContext wsContext = await listenerContext.AcceptWebSocketAsync(null);

                await clientConnectTask;

                // Ensure websocket is connected from server.
                Assert.Equal(WebSocketState.Open, wsContext.WebSocket.State);

                // Websocket subProtocol.
                Assert.Null(wsContext.WebSocket.SubProtocol);

                const string expected = "hello";
                byte[] receiveBuffer = Encoding.UTF8.GetBytes(expected);

                // Send binary data from server.
                await wsContext.WebSocket.SendAsync(new ArraySegment<byte>(receiveBuffer), WebSocketMessageType.Binary, true, CancellationToken.None);

                // Receive binary data in client.
                ArraySegment<byte> getBuffer = new ArraySegment<byte>(new byte[receiveBuffer.Length]);
                await clientWebSocket.ReceiveAsync(getBuffer, CancellationToken.None);

                Assert.Equal(expected, Encoding.UTF8.GetString(getBuffer.Array));
            }
        }
    }
}
