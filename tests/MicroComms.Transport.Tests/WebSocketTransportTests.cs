using FluentAssertions;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace MicroComms.Transport.Tests;

public class WebSocketTransportTests
{
    private static int GetFreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    [Fact]
    public async Task ClientTransport_EchoesMessage_OnMessageReceivedFires()
    {
        // Arrange: spin up an HttpListener that accepts one WS connection and echoes.
        var port = GetFreePort();
        var prefix = $"http://localhost:{port}/ws/";
        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        var serverTask = Task.Run(async () =>
        {
            try
            {
                var ctx = await listener.GetContextAsync();
                var wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null);
                var socket = wsCtx.WebSocket;

                // Receive one message
                var buffer = new byte[1024];
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                var received = new byte[result.Count];
                Array.Copy(buffer, received, result.Count);

                // Echo it back
                await socket.SendAsync(received, WebSocketMessageType.Binary, true, CancellationToken.None);
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
            catch (Exception ex)
            {
                var i = 0;
            }
        });

        // Act: connect transport to the echo server
        var transport = new ClientTransport(new Uri($"ws://localhost:{port}/ws/"));

        bool connectedFired = false;
        bool disconnectedFired = false;
        byte[]? echo = null;

        transport.OnConnected += () => connectedFired = true;
        transport.OnMessageReceived += data =>
        {
            echo = data;
            return Task.CompletedTask;
        };
        transport.OnDisconnected += () => disconnectedFired = true;

        await transport.ConnectAsync();

        // Assert OnConnected
        connectedFired.Should().BeTrue();

        // Send a payload and wait for the echo
        var payload = new byte[] { 9, 8, 7, 6 };
        await transport.SendAsync(payload);

        var sw = Stopwatch.StartNew();
        while (echo == null && sw.Elapsed < TimeSpan.FromSeconds(5))
            await Task.Delay(10);

        echo.Should().NotBeNull().And.Equal(payload);

        // Now close the transport and wait for OnDisconnected
        await transport.StopAsync();
        sw.Restart();
        while (!disconnectedFired && sw.Elapsed < TimeSpan.FromSeconds(1))
            await Task.Delay(10);

        disconnectedFired.Should().BeTrue();

        // Cleanup
        listener.Stop();
        await serverTask;
    }
}