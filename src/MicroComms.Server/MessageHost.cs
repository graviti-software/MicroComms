using MicroComms.Core.Abstractions;
using MicroComms.Core.Models;
using MicroComms.Serialization.MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection; // Add this using directive
using Microsoft.Extensions.Hosting;
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

    private readonly IHost _host;
    private readonly ISerializer _serializer;
    private readonly List<IMessageInterceptor> _interceptors = [];

    private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _handlers
        = new();

    private readonly List<Task> _clientTasks = [];

    /// <summary>Fired when a new client connects (remote endpoint).</summary>
    public event Action<IPAddress>? Connected;

    /// <summary>Fired when a client disconnects.</summary>
    public event Action<IPAddress>? Disconnected;

    public MessageHost(string urlPrefix, ISerializer? serializer = null)
    {
        _host = Host.CreateDefaultBuilder()
          .ConfigureWebHostDefaults(webBuilder =>
          {
              webBuilder.ConfigureServices(services =>
              {
                  // add WebSocket support
                  services.AddWebSockets(options =>
                  {
                      options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                  });
              });
              webBuilder.UseUrls(urlPrefix)
                        .Configure(app =>
                        {
                            // map /ws to your WebSocket handler
                            app.UseWebSockets();
                            app.Map("/ws", HandleWebSocket);
                        });
          })
          .Build();

        _serializer = serializer ?? new MessagePackSerializerAdapter();
    }

    private void HandleWebSocket(IApplicationBuilder builder)
    {
        builder.Use(async (HttpContext context, RequestDelegate next) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var transport = new ServerWebSocketTransport(webSocket);
                transport.OnMessageReceived += data => HandleClientMessageAsync(transport, data);
                transport.OnConnected += () => Connected?.Invoke(context.Connection.RemoteIpAddress!);
                transport.OnDisconnected += () => Disconnected?.Invoke(context.Connection.RemoteIpAddress!);
                // run a receive loop per‐client
                var loopTask = transport.ReceiveLoopAsync(context.RequestAborted);
                _clientTasks.Add(loopTask);

                await loopTask; // wait for the loop to finish
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });
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
            _ => [wrapper],
            (_, list) => { list.Add(wrapper); return list; });
    }

    /// <summary>Start listening for WebSocket clients.</summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        await _host.StartAsync(ct);
    }

    /// <summary>Stop listening and disconnect all clients.</summary>
    public async Task StopAsync()
    {
        await _host.StopAsync();
        await Task.WhenAll(_clientTasks);
    }

    public void Dispose() => _host.Dispose();

    // send back to one client
    private Task SendToClientAsync(ITransport transport, byte[] data)
        => transport.SendAsync(data);

    // called on each message from a specific client
    private async Task HandleClientMessageAsync(
        ITransport transport,
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
        var ack = new Response(
            frame.Id,
            status,
            error
        );

        var ackPayload = _serializer.Serialize(ack);
        var ackFrame = new MessageFrame(
            Guid.NewGuid(),
            typeof(Response).AssemblyQualifiedName!,
            ackPayload
        );

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