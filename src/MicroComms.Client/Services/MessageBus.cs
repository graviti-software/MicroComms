using MicroComms.Client.Models;
using MicroComms.Core.Abstractions;
using MicroComms.Core.Models;
using System.Collections.Concurrent;

namespace MicroComms.Client.Services;

internal class MessageBus : IMessageBus
{
    private readonly ITransport _transport;
    private readonly ISerializer _serializer;
    private readonly List<IMessageInterceptor> _interceptors = [];
    private readonly ConcurrentDictionary<string, List<Func<object, Task>>> _handlers = [];
    private bool _disposed;

    // Tracks in-flight requests by message Id
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Response>> _pending = [];

    private readonly int _reconnectDelay;

    public event Action? Connected;

    public event Action? Disconnected;

    public event Action? Reconnecting;

    public MessageBus(ITransport transport, ISerializer serializer, int reconnectDelay)
    {
        _transport = transport;
        _serializer = serializer;

        // wire lifecycle
        _transport.OnConnected += () => Connected?.Invoke();

        _transport.OnDisconnected += async () => await HandleDisconnectionAsync();

        _transport.OnMessageReceived += HandleRawMessageAsync;
        _reconnectDelay = reconnectDelay;
    }

    private async Task HandleDisconnectionAsync()
    {
        Disconnected?.Invoke();
        Reconnecting?.Invoke();
        // simple backoff/reconnect
        await Task.Delay(_reconnectDelay);
        await _transport.ConnectAsync();
    }

    public void UseInterceptor(IMessageInterceptor interceptor)
        => _interceptors.Add(interceptor);

    public void Subscribe<T>(Func<T, Task> handler)
    {
        var key = typeof(T).AssemblyQualifiedName!;
        var wrapper = new Func<object, Task>(o => handler((T)o));
        _handlers.AddOrUpdate(key,
            _ => [wrapper],
            (_, list) => { list.Add(wrapper); return list; });
    }

    public async Task SendAsync<T>(T message, CancellationToken ct = default)
    {
        // 1) Build frame
        var frame = new MessageFrame(
            Guid.NewGuid(),
            typeof(T).AssemblyQualifiedName!,
            _serializer.Serialize(message)
        );

        // 2) Intercept outgoing
        var env = new MessageEnvelope(
            frame.Id,
            frame.Type,
            frame.Payload
        );

        foreach (var ix in _interceptors)
            await ix.OnSendingAsync(env);

        // 3) Serialize frame & send
        var bytes = _serializer.Serialize(frame);
        await _transport.SendAsync(bytes, ct);
    }

    private async Task HandleRawMessageAsync(byte[] data)
    {
        // 1) Deserialize frame
        var frame = _serializer.Deserialize<MessageFrame>(data);

        // 2) Is this an ACK?
        var ackTypeName = typeof(Response).AssemblyQualifiedName!;
        if (frame.Type == ackTypeName)
        {
            var ack = _serializer.Deserialize<Response>(frame.Payload);
            if (_pending.TryRemove(ack.CorrelationId, out var tcs))
                tcs.TrySetResult(ack);
            return;      // do not dispatch further
        }

        // 3) Normal intercept
        var env = new MessageEnvelope(
            frame.Id,
            frame.Type,
            frame.Payload
        );
        foreach (var ix in _interceptors)
            await ix.OnReceivedAsync(env);

        // 4) Dispatch to subscribers
        if (_handlers.TryGetValue(frame.Type, out var list))
        {
            var messageType = Type.GetType(frame.Type)!;
            var payload = _serializer.Deserialize(messageType, frame.Payload);
            foreach (var handler in list)
                _ = handler(payload);
        }
    }

    public async Task<Response> RequestAsync<T>(T message, CancellationToken ct = default)
    {
        // 1) Prepare envelope
        var id = Guid.NewGuid();
        var frame = new MessageFrame(
            id,
            typeof(T).AssemblyQualifiedName!,
            _serializer.Serialize(message)
        );

        // 2) Create a TCS and register cancellation
        var tcs = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = ct.Register(() => tcs.TrySetCanceled(ct));

        if (!_pending.TryAdd(id, tcs))
            throw new InvalidOperationException("ID collision on pending requests.");

        // 3) Intercept & send
        var env = new MessageEnvelope(
            frame.Id,
            frame.Type,
            frame.Payload
        );
        foreach (var ix in _interceptors)
            await ix.OnSendingAsync(env);

        var bytes = _serializer.Serialize(frame);
        await _transport.SendAsync(bytes, ct);

        // 4) Await the ACK
        return await tcs.Task;
    }

    public Task<Response<TPayload>> RequestAsync<TRequest, TPayload>(TRequest message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _transport.StopAsync().GetAwaiter().GetResult();
                _interceptors.Clear();
                _handlers.Clear();
                _pending.Clear();
            }
            // No unmanaged resources to release
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}