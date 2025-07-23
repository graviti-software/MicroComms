using MicroComms.Transport.Abstractions;
using System.Net.WebSockets;

namespace MicroComms.Transport;

/// <summary>
/// WebSocket-based transport for clients.
/// </summary>
public class ClientTransport(Uri endpoint) : IWebSocketTransport
{
    private readonly Uri _endpoint = endpoint;
    private readonly ClientWebSocket _socket = new();

    public Uri Endpoint => _endpoint;

    public event Func<byte[], Task> OnMessageReceived = _ => Task.CompletedTask;

    public event Action? OnConnected;

    public event Action? OnDisconnected;

    private Task? _receiveTask = null;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _socket.ConnectAsync(_endpoint, cancellationToken);
        OnConnected?.Invoke();
        _receiveTask = Task.Run(() => ReceiveLoop(cancellationToken), cancellationToken);
    }

    public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        => _socket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_socket.State != WebSocketState.Open)
            return Task.CompletedTask; // already closed or not connected
        return _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", cancellationToken);
    }

    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        while (_socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, cancellationToken);

                // *** ignore close frames (and any non-binary) ***
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // gracefully exit the loop on close
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", cancellationToken);
                    OnDisconnected?.Invoke();
                    return;
                }

                // only accumulate binary payload
                if (result.MessageType == WebSocketMessageType.Binary)
                    await ms.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            } while (!result.EndOfMessage);

            // only fire for real binary messages
            var payload = ms.ToArray();
            if (payload.Length > 0)
            {
                await OnMessageReceived(payload);
            }
        }

        OnDisconnected?.Invoke();
    }
}