using MicroComms.Core.Abstractions;

namespace MicroComms.Transport.WebSocket;

public static class WebsocketTransportExtensions
{
    public static ITransport CreateWebSocketTransport(this Uri endpoint)
        => new WebsocketTransport(endpoint);
}