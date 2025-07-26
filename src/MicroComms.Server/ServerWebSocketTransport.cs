using MicroComms.Core;
using System.Net.WebSockets;

namespace MicroComms.Server;

internal class ServerWebSocketTransport : ITransport
{
    private readonly WebSocket _socket;

    // the existing event
    public event Func<byte[], Task> OnMessageReceived = _ => Task.CompletedTask;

    // ← add these two:
    public event Action? OnConnected;

    public event Action? OnDisconnected;

    public ServerWebSocketTransport(WebSocket socket)
    {
        _socket = socket;
        // fire connected immediately since this ctor runs after AcceptWebSocketAsync
        OnConnected?.Invoke();
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask; // already “connected”

    public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        => _socket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_socket.State == WebSocketState.Open)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", cancellationToken);
        }
        OnDisconnected?.Invoke(); // already closed or not connected
    }

    public async Task ReceiveLoopAsync(CancellationToken cancellationToken)
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
                    await DisconnectAsync(cancellationToken);
                    return;
                }

                await ms.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
            } while (!result.EndOfMessage);

            // only fire for real binary messages
            var payload = ms.ToArray();
            if (payload.Length > 0)
            {
                await OnMessageReceived(payload);
            }
        }

        // when loop exits, signal disconnect

        await DisconnectAsync(cancellationToken);
    }
}