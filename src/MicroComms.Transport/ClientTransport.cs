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

    public async Task ConnectAsync(Uri endpoint, CancellationToken cancellationToken = default)
    {
        await _socket.ConnectAsync(_endpoint, cancellationToken);
        OnConnected?.Invoke();
        _ = Task.Run(() => ReceiveLoop(cancellationToken), cancellationToken);
    }

    public Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        => _socket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", cancellationToken);

    private async Task ReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[4096];
        while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult res;
            do
            {
                res = await _socket.ReceiveAsync(buffer, ct);
                await ms.WriteAsync(buffer.AsMemory(0, res.Count), ct);
            } while (!res.EndOfMessage);

            await OnMessageReceived(ms.ToArray());
        }

        OnDisconnected?.Invoke();
    }
}