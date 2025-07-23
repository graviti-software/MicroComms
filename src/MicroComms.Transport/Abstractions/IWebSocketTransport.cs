namespace MicroComms.Transport.Abstractions;

/// <summary>
/// Low-level WebSocket send/receive API.
/// </summary>
public interface IWebSocketTransport
{
    /// <summary>Connect to the remote endpoint.</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Send raw bytes.</summary>
    Task SendAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>Fires when raw bytes are received.</summary>
    event Func<byte[], Task> OnMessageReceived;

    /// <summary>Fired once the WS handshake succeeds.</summary>
    event Action? OnConnected;

    /// <summary>Fired when the socket closes (normal or error).</summary>
    event Action? OnDisconnected;

    /// <summary>Gracefully stop the connection.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}