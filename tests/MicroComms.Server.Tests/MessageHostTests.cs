using FluentAssertions;
using MicroComms.Client.Services;
using MicroComms.Core.Abstractions;
using MicroComms.Serialization.Adapters;
using MicroComms.Transport;
using System.Net;
using System.Net.Sockets;

namespace MicroComms.Server.Tests;

public class MessageHostTests
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
    public async Task Host_DispatchesMessage_And_ClientReceivesAck()
    {
        // Arrange
        var port = GetFreePort();
        var urlPrefix = $"http://localhost:{port}/ws/";
        var wsEndpoint = new Uri($"ws://localhost:{port}/ws/");

        // Start the host
        var host = new MessageHost(urlPrefix, new MessagePackSerializerAdapter());
        bool handlerRan = false;
        host.Subscribe<TestMessage>(async msg =>
        {
            handlerRan = msg.Value == 1234;
            await Task.CompletedTask;
        });
        using var cts = new CancellationTokenSource();
        _ = host.StartAsync(cts.Token);

        // Give the HTTP listener a moment to spin up
        await Task.Delay(200);

        // Wire client
        var transport = new ClientTransport(wsEndpoint);
        var client = new MessageClient(
            transport,
            new MessagePackSerializerAdapter(),
            // use null‐logger to satisfy ctor
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MessageClient>.Instance,
            reconnectDelay: 0
        );

        Ack? ackResult = null;
        transport.OnConnected += () => { /* ok */ };
        transport.OnDisconnected += () => { /* ok */ };

        // Act: open the socket…
        await transport.ConnectAsync();

        // Act: send a TestMessage and await ack
        ackResult = await client.RequestAsync(new TestMessage { Value = 1234 });

        // Assert
        handlerRan.Should().BeTrue("the host should have invoked the TestMessage handler");
        ackResult.Should().NotBeNull();
        ackResult!.StatusCode.Should().Be(200);

        // Cleanup
        await cts.CancelAsync();
        await transport.StopAsync();
    }

    // Simple DTO
    private class TestMessage
    {
        public int Value { get; set; }
    }
}