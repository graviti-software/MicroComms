namespace MicroComms.Core.Abstractions;

/// <summary>
/// Defines the message bus API for handling messages.
/// </summary>
public interface IMessageBus : IDisposable
{
    /// <summary>Handle incoming messages of type T.</summary>
    void Subscribe<T>(Func<T, Task> handler);

    /// <summary>Attach middleware for logging, auth, etc.</summary>
    void UseInterceptor(IMessageInterceptor interceptor);

    /// <summary>Fired when the WS connection is up.</summary>
    event Action? Connected;

    /// <summary>Fired when the WS connection is closed.</summary>
    event Action? Disconnected;

    /// <summary>Fired on each reconnect attempt.</summary>
    event Action? Reconnecting;
}