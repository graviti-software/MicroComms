using MicroComms.Core;
using MicroComms.Core.Abstractions;
using MicroComms.Core.Models;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace MicroComms.Transport.WebSocket;

/// <summary>
/// Pure-binary WebSocket transport: sends raw bytes & returns raw bytes.
/// Implements the full IDisposable pattern.
/// </summary>
public class WebSocketTransport : ITransport, IDisposable
{
    private readonly Uri _endpoint;
    private readonly ClientWebSocket _socket = new();
    private readonly ISerializer _serializer;

    // ➊ Map envelope IDs → awaiting callers
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<byte[]>> _pending = [];

    // ➋ Token to cancel the receive loop
    private readonly CancellationTokenSource _receiverCts = new();

    // ➌ Kick off the loop
#pragma warning disable S4487 // Unread "private" fields should be removed
    private readonly Task _receiverTask;
#pragma warning restore S4487 // Unread "private" fields should be removed

    private bool _disposed;

    public WebSocketTransport(Uri endpoint,
        ISerializer serializer)
    {
        _receiverTask = Task.Run(() => ReceiveLoopAsync(_receiverCts.Token));
        _endpoint = endpoint;
        _serializer = serializer;
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_socket.State == WebSocketState.Open) return;
        await _socket.ConnectAsync(_endpoint, cancellationToken);
    }

    /// <summary>
    /// Continuously read full messages, parse the envelope,
    /// and complete the matching TCS in _pending.
    /// </summary>
    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectedAsync(cancellationToken);

        var buffer = new byte[4096];
        while (!cancellationToken.IsCancellationRequested)
        {
            // read until EndOfMessage
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken);
                await ms.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            }
            while (!result.EndOfMessage);

            var raw = ms.ToArray();

            // parse envelope to get the correlation Id
            MessageEnvelope envelope;
            try
            {
                envelope = _serializer.Deserialize<MessageEnvelope>(raw);
            }
            catch
            {
                // malformed envelope; ignore or log
                continue;
            }

            // if someone is waiting, complete their TCS
            if (_pending.TryRemove(envelope.Id, out var tcs))
                tcs.TrySetResult(raw);
            // else drop it or log
        }
    }

    public async Task<byte[]> SendAsync(
        byte[] data,
        CancellationToken cancellationToken = default
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(WebSocketTransport));

        // 1) Deserialize the request envelope so we can get its Id
        var requestEnv = _serializer.Deserialize<MessageEnvelope>(data);
        var id = requestEnv.Id;

        // 2) Prepare a TCS and register it
        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pending.TryAdd(id, tcs))
            throw new InvalidOperationException($"Duplicate envelope Id: {id}");

        // 3) Send the raw bytes
        await EnsureConnectedAsync(cancellationToken);
        await _socket.SendAsync(
            new ArraySegment<byte>(data),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            cancellationToken
        );

        // 4) Await response or cancellation
        using var ctr = cancellationToken.Register(() =>
        {
            if (_pending.TryRemove(id, out var pendingTcs))
            {
                pendingTcs.TrySetCanceled();
            }
        });

        // If cancelled, TCS will be faulted
        return await tcs.Task;
    }

    #region IDisposable Support

    // Destructor only needed if you have unmanaged resources
    ~WebSocketTransport()
    {
        Dispose(disposing: false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // stop the receive loop
            _receiverCts.Cancel();
            try { _receiverTask.GetAwaiter().GetResult(); }
            catch { /* swallow */ }

            _socket.Dispose();
            _receiverCts.Dispose();
        }

        // free unmanaged resources (none here)

        _disposed = true;
    }

    /// <summary>
    /// Dispose of the socket and suppress finalization.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion IDisposable Support
}