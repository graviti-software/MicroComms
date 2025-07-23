using MicroComms.Transport.Abstractions;
using System.Net.WebSockets;

namespace MicroComms.Server;

internal class ServerWebSocketTransport : IWebSocketTransport
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

    public Task ConnectAsync(Uri _, CancellationToken __ = default)
        => Task.CompletedTask; // already “connected”

    public Task SendAsync(byte[] data, CancellationToken ct = default)
        => _socket.SendAsync(data, WebSocketMessageType.Binary, true, ct);

    public Task StopAsync(CancellationToken ct = default)
        => _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closing", ct);

    public async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, ct);
                await ms.WriteAsync(buffer.AsMemory(0, result.Count), ct);
            } while (!result.EndOfMessage);

            await OnMessageReceived(ms.ToArray());
        }

        // when loop exits, signal disconnect
        OnDisconnected?.Invoke();
    }
}