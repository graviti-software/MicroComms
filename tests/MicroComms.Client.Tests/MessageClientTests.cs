using FluentAssertions;
using MicroComms.Client.Models;
using MicroComms.Client.Services;
using MicroComms.Core.Abstractions;
using MicroComms.Serialization.Adapters;
using MicroComms.Transport.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace MicroComms.Client.Tests;

// A simple fake transport for unit tests
internal class FakeTransport : IWebSocketTransport
{
    public byte[]? LastSent { get; private set; }

    public event Func<byte[], Task> OnMessageReceived = _ => Task.CompletedTask;

    public event Action? OnConnected;

    public event Action? OnDisconnected;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        OnConnected?.Invoke();
        return Task.CompletedTask;
    }

    public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        LastSent = data;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        OnDisconnected?.Invoke();
        return Task.CompletedTask;
    }

    /// <summary>Helper to simulate an incoming message.</summary>
    public Task RaiseMessageReceived(byte[] data)
        => OnMessageReceived.Invoke(data);
}

// A minimal test DTO for messaging
internal class TestMessage
{
    public int Value { get; set; }
}

// Inline interceptor for observing calls
internal class InlineInterceptor : IMessageInterceptor
{
    public Func<IMessageEnvelope, Task>? OnSending { get; set; }
    public Func<IMessageEnvelope, Task>? OnReceived { get; set; }

    public Task OnSendingAsync(IMessageEnvelope envelope)
        => OnSending?.Invoke(envelope) ?? Task.CompletedTask;

    public Task OnReceivedAsync(IMessageEnvelope envelope)
        => OnReceived?.Invoke(envelope) ?? Task.CompletedTask;
}

public class MessageClientTests
{
    [Fact]
    public async Task SendAsync_SerializeAndIntercept()
    {
        var transport = new FakeTransport();
        var serializer = new MessagePackSerializerAdapter();
        var client = new MessageClient(
            transport,
            serializer,
            NullLogger<MessageClient>.Instance,
            reconnectDelay: 0
        );

        bool sawOnSending = false;
        client.UseInterceptor(new InlineInterceptor
        {
            OnSending = env => { sawOnSending = true; return Task.CompletedTask; }
        });

        await client.SendAsync(new TestMessage { Value = 123 });

        sawOnSending.Should().BeTrue("the OnSending interceptor should be called");

        // Verify the frame on the wire
        var rawFrame = transport.LastSent!;
        var frame = serializer.Deserialize<MessageFrame>(rawFrame);
        frame.Type.Should().Be(typeof(TestMessage).AssemblyQualifiedName);
        var payload = serializer.Deserialize<TestMessage>(frame.Payload);
        payload.Value.Should().Be(123);
    }

    [Fact]
    public async Task Subscribe_DispatchesIncomingMessages()
    {
        var transport = new FakeTransport();
        var serializer = new MessagePackSerializerAdapter();
        var client = new MessageClient(
            transport,
            serializer,
            NullLogger<MessageClient>.Instance,
            reconnectDelay: 0
        );

        int received = 0;
        client.Subscribe<TestMessage>(msg =>
        {
            received = msg.Value;
            return Task.CompletedTask;
        });

        // Build a wire frame and pump it through
        var inboundFrame = new MessageFrame
        {
            Id = Guid.NewGuid(),
            Type = typeof(TestMessage).AssemblyQualifiedName!,
            Payload = serializer.Serialize(new TestMessage { Value = 456 })
        };
        var raw = serializer.Serialize(inboundFrame);

        await transport.RaiseMessageReceived(raw);

        received.Should().Be(456);
    }

    [Fact]
    public async Task RequestAsync_CompletesWhenAckArrives()
    {
        var transport = new FakeTransport();
        var serializer = new MessagePackSerializerAdapter();
        var client = new MessageClient(
            transport,
            serializer,
            NullLogger<MessageClient>.Instance,
            reconnectDelay: 0
        );

        // Fire off the request
        var pending = client.RequestAsync(new TestMessage { Value = 789 });

        // Grab the sent frame
        var sentFrame = serializer.Deserialize<MessageFrame>(transport.LastSent!);

        // Simulate the peer sending back an ACK
        var ack = new Ack
        {
            CorrelationId = sentFrame.Id,
            StatusCode = 200,
            ErrorMessage = null
        };
        var ackFrame = new MessageFrame
        {
            Id = Guid.NewGuid(),
            Type = typeof(Ack).AssemblyQualifiedName!,
            Payload = serializer.Serialize(ack)
        };
        var rawAck = serializer.Serialize(ackFrame);

        await transport.RaiseMessageReceived(rawAck);

        var result = await pending;
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Interceptor_OnReceived_IsCalledBeforeHandler()
    {
        // Arrange
        var transport = new FakeTransport();
        var serializer = new MessagePackSerializerAdapter();
        var client = new MessageClient(
            transport,
            serializer,
            NullLogger<MessageClient>.Instance,
            reconnectDelay: 0
        );

        bool sawInterceptor = false;
        bool sawHandler = false;

        // Interceptor sets sawInterceptor
        client.UseInterceptor(new InlineInterceptor
        {
            OnReceived = env =>
            {
                sawInterceptor = true;
                return Task.CompletedTask;
            }
        });

        // Subscriber will only flip sawHandler if interceptor already ran
        client.Subscribe<TestMessage>(msg =>
        {
            sawHandler = sawInterceptor;
            return Task.CompletedTask;
        });

        // Build a wire‐frame for TestMessage
        var frame = new MessageFrame
        {
            Id = Guid.NewGuid(),
            Type = typeof(TestMessage).AssemblyQualifiedName!,
            Payload = serializer.Serialize(new TestMessage { Value = 999 })
        };
        var raw = serializer.Serialize(frame);

        // Act: simulate incoming bytes
        await transport.RaiseMessageReceived(raw);

        // Assert
        sawInterceptor.Should().BeTrue("the OnReceived interceptor should have run");
        sawHandler.Should().BeTrue("the handler should see that the interceptor already ran");
    }
}