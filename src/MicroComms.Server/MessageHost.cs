using MicroComms.Client.Models;
using MicroComms.Core.Abstractions;
using MicroComms.Serialization.Adapters;
using MicroComms.Transport.Abstractions;
using System.Collections.Concurrent;
using System.Net;

namespace MicroComms.Server;

/// <summary>
/// Listens for WebSocket clients and dispatches messages via the same bus API.
/// </summary>
public class MessageHost : IDisposable
{
    // simple, internal envelope impl for interceptor calls
    private sealed class EnvelopeImpl : IMessageEnvelope
    {
        public Guid Id { get; init; }
        public string Type { get; init; } = null!;
        public byte[] Payload { get; init; } = null!;
    }

    private readonly HttpListener _listener;
    private readonly ISerializer _serializer;
    private readonly List<IMessageInterceptor> _interceptors = new();

    private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _handlers
        = new();

    private readonly List<Task> _clientTasks = new();

    /// <summary>Fired when a new client connects (remote endpoint).</summary>
    public event Action<EndPoint>? Connected;

    /// <summary>Fired when a client disconnects.</summary>
    public event Action<EndPoint>? Disconnected;

    public MessageHost(string urlPrefix, ISerializer? serializer = null)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(urlPrefix);
        _serializer = serializer ?? new MessagePackSerializerAdapter();
    }

    /// <summary>Add middleware (logging, auth, etc.).</summary>
    public void UseInterceptor(IMessageInterceptor interceptor)
        => _interceptors.Add(interceptor);

    /// <summary>Subscribe to messages of type T from any client.</summary>
    public void Subscribe<T>(Func<T, Task> handler)
    {
        var key = typeof(T).AssemblyQualifiedName!;
        var wrapper = new Func<object, Task>(o => handler((T)o));
        _handlers.AddOrUpdate(key,
            _ => new List<Func<object, Task>> { wrapper },
            (_, list) => { list.Add(wrapper); return list; });
    }

    /// <summary>Start listening for WebSocket clients.</summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _listener.Start();
        while (!ct.IsCancellationRequested)
        {
            var ctx = await _listener.GetContextAsync();
            if (!ctx.Request.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                ctx.Response.Close();
                continue;
            }

            var wsCtx = await ctx.AcceptWebSocketAsync(null);
            var webSocket = wsCtx.WebSocket;
            Connected?.Invoke(ctx.Request.RemoteEndPoint);

            // wrap it in our transport
            var transport = new ServerWebSocketTransport(webSocket);
            transport.OnMessageReceived += data => HandleClientMessageAsync(transport, data);

            // run a receive loop per‐client
            _clientTasks.Add(Task.Run(() => transport.ReceiveLoopAsync(ct), ct));
        }
    }

    /// <summary>Stop listening and disconnect all clients.</summary>
    public async Task StopAsync()
    {
        _listener.Stop();
        await Task.WhenAll(_clientTasks);
    }

    public void Dispose() => _ = StopAsync();

    // send back to one client
    private Task SendToClientAsync(IWebSocketTransport transport, byte[] data)
        => transport.SendAsync(data);

    // called on each message from a specific client
    private async Task HandleClientMessageAsync(
        IWebSocketTransport transport,
        byte[] data
    )
    {
        // 1) Deserialize incoming frame
        var frame = _serializer.Deserialize<MessageFrame>(data);

        // 2) Intercept arrival
        var incomingEnv = new EnvelopeImpl
        {
            Id = frame.Id,
            Type = frame.Type,
            Payload = frame.Payload
        };
        foreach (var ix in _interceptors)
            await ix.OnReceivedAsync(incomingEnv);

        // 3) Dispatch to any subscribers
        var status = 200;
        string? error = null;

        if (_handlers.TryGetValue(frame.Type, out var handlers))
        {
            var msgType = Type.GetType(frame.Type)!;
            var payload = _serializer.Deserialize(msgType, frame.Payload);

            foreach (var handler in handlers)
            {
                try
                {
                    await handler(payload);
                }
                catch (Exception ex)
                {
                    status = 500;
                    error = ex.Message;
                }
            }
        }
        else
        {
            // no subscribers = 404
            status = 404;
            error = $"No handler for message type {frame.Type}";
        }

        // 4) Build and send back the ACK frame
        var ack = new Ack
        {
            CorrelationId = frame.Id,
            StatusCode = status,
            ErrorMessage = error
        };

        var ackPayload = _serializer.Serialize(ack);
        var ackFrame = new MessageFrame
        {
            Id = Guid.NewGuid(),
            Type = typeof(Ack).AssemblyQualifiedName!,
            Payload = ackPayload
        };

        // 5) Intercept outgoing ACK
        var outgoingEnv = new EnvelopeImpl
        {
            Id = ackFrame.Id,
            Type = ackFrame.Type,
            Payload = ackFrame.Payload
        };
        foreach (var ix in _interceptors)
            await ix.OnSendingAsync(outgoingEnv);

        // 6) Send it
        var bytes = _serializer.Serialize(ackFrame);
        await transport.SendAsync(bytes);
    }
}