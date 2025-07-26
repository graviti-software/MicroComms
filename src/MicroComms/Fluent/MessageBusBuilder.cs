using MicroComms.Core.Abstractions;
using MicroComms.Serialization.MessagePack;
using MicroComms.Services;

namespace MicroComms.Fluent;

/// <summary>
/// Fluent builder for IMessageBus (MessageClient).
/// </summary>
public class MessageBusBuilder
{
    private ISerializer _serializer = new MessagePackSerializerAdapter();
    private ITransport? _transport;
    private readonly List<IMessageInterceptor> _interceptors = [];
    private readonly List<Action> _onConnectedHandlers = [];
    private readonly List<Action> _onDisconnectedHandlers = [];
    private readonly List<Action> _onReconnectingHandlers = [];
    private int _reconnectDelay = 2000;

    public MessageBusBuilder WithTransport(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport), "Transport cannot be null.");
        return this;
    }

    /// <summary>Override the default serializer (MessagePack).</summary>
    public MessageBusBuilder WithSerializer(ISerializer serializer)
    {
        _serializer = serializer;
        return this;
    }

    /// <summary>Customize the reconnect back-off in milliseconds (default: 2000).</summary>
    public MessageBusBuilder WithReconnectDelay(int milliseconds)
    {
        if (milliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Reconnect delay must be greater than zero.");
        }
        _reconnectDelay = milliseconds;
        return this;
    }

    /// <summary>Add an interceptor (logging, auth, etc.).</summary>
    public MessageBusBuilder AddInterceptor(IMessageInterceptor interceptor)
    {
        if (interceptor == null)
        {
            throw new ArgumentNullException(nameof(interceptor), "Interceptor cannot be null.");
        }
        _interceptors.Add(interceptor);
        return this;
    }

    /// <summary>Register a handler for the Connected event.</summary>
    public MessageBusBuilder OnConnected(Action handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "Handler cannot be null.");
        }
        _onConnectedHandlers.Add(handler);
        return this;
    }

    /// <summary>Register a handler for the Disconnected event.</summary>
    public MessageBusBuilder OnDisconnected(Action handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "Handler cannot be null.");
        }
        _onDisconnectedHandlers.Add(handler);
        return this;
    }

    /// <summary>Register a handler for the Reconnecting event.</summary>
    public MessageBusBuilder OnReconnecting(Action handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler), "Handler cannot be null.");
        }
        _onReconnectingHandlers.Add(handler);
        return this;
    }

    /// <summary>Builds and returns the configured IMessageBus.</summary>
    public async Task<IMessageBus> BuildAsync()
    {
        if (_transport == null)
        {
            throw new InvalidOperationException("Transport must be set before building the MessageBus.");
        }
        var client = new MessageBus(_transport,
            _serializer,
            _reconnectDelay);

        // wire events
        foreach (var h in _onConnectedHandlers) client.Connected += h;
        foreach (var h in _onDisconnectedHandlers) client.Disconnected += h;
        foreach (var h in _onReconnectingHandlers) client.Reconnecting += h;

        // add interceptors
        foreach (var ix in _interceptors) client.UseInterceptor(ix);

        // kick off connection
        await _transport.ConnectAsync();

        return client;
    }

    /// <summary>Builds and returns the configured IMessageBus synchronously.</summary>
    public IMessageBus Build()
    {
        return BuildAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }
}