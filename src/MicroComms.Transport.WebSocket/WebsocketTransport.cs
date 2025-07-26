using MicroComms.Core.Abstractions;
using System.Net.WebSockets;

namespace MicroComms.Transport.WebSocket;

internal class WebsocketTransport(Uri endpoint) : ITransport
{
    private readonly Uri _endpoint = endpoint;
    private readonly ClientWebSocket _socket = new();

    public Uri Endpoint => _endpoint;

    public event Func<byte[], Task> OnMessageReceived = _ => Task.CompletedTask;

    public event Action? OnConnected;

    public event Action? OnDisconnected;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _socket.ConnectAsync(_endpoint, cancellationToken);
        OnConnected?.Invoke();
        _ = Task.Run(() => ReceiveLoop(cancellationToken), cancellationToken);
    }

    public async Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
        => await _socket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_socket.State == WebSocketState.Open)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", cancellationToken);
        }
        OnDisconnected?.Invoke(); // already closed or not connected
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
                    await StopAsync(cancellationToken);
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

        await StopAsync(cancellationToken);
    }
}