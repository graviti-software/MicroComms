using MicroComms.Core;
using MicroComms.Core.Abstractions;

namespace MicroComms.Fluent;

/// <summary>
/// Fluent builder for console apps (no DI). Configure transports, serializers, interceptors, and build a mediator.
/// </summary>
public class MicroCommsBuilder
{
    private readonly Dictionary<Type, object> _handlers = [];
    private readonly Dictionary<Type, List<object>> _interceptors = [];

    private MicroCommsBuilder()
    { }

    /// <summary>
    /// Create a new builder instance.
    /// </summary>
    public static MicroCommsBuilder Create() => new();

    /// <summary>
    /// Map a raw ITransport and ISerializer to handle a specific request/response pair.
    /// </summary>
    public MicroCommsBuilder MapTransport<TRequest, TResponse>(
        ITransport transport,
        ISerializer serializer
    )
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(serializer);

        var handler = new MessageHandler<TRequest, TResponse>(transport, serializer);
        _handlers[typeof(TRequest)] = handler;
        return this;
    }

    /// <summary>
    /// Register an interceptor for a specific request/response pair.
    /// </summary>
    public MicroCommsBuilder UseInterceptor<TRequest, TResponse>(
        IMessageInterceptor<TRequest, TResponse> interceptor
    )
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));
        var key = typeof(TRequest);
        if (!_interceptors.TryGetValue(key, out var list))
        {
            list = new List<object>();
            _interceptors[key] = list;
        }
        list.Add(interceptor);
        return this;
    }

    /// <summary>
    /// Build an IRequestMediator backed by the configured handlers and interceptors.
    /// </summary>
    public IRequestMediator Build()
    {
        return new InMemoryMediator(_handlers, _interceptors);
    }

    // Internal mediator implementation for console apps
    private class InMemoryMediator : IRequestMediator
    {
        private readonly Dictionary<Type, object> _handlers;
        private readonly Dictionary<Type, List<object>> _interceptors;

        public InMemoryMediator(
            Dictionary<Type, object> handlers,
            Dictionary<Type, List<object>> interceptors
        )
        {
            _handlers = handlers;
            _interceptors = interceptors;
        }

        public Task<TResponse> SendAsync<TRequest, TResponse>(
            TRequest request,
            CancellationToken cancellationToken = default
        )
            where TRequest : IRequest<TResponse>
            where TResponse : IResponse
        {
            if (!_handlers.TryGetValue(typeof(TRequest), out var rawHandler) ||
                rawHandler is not ITransportHandler<TRequest, TResponse> handler)
            {
                throw new InvalidOperationException(
                    $"No handler registered for {typeof(TRequest).Name}"
                );
            }

            // Core handler
            Func<TRequest, CancellationToken, Task<TResponse>> pipeline = handler.SendAsync;

            // Wrap with interceptors if any
            if (_interceptors.TryGetValue(typeof(TRequest), out var list))
            {
                foreach (var rawInterceptor in list)
                {
                    var interceptor = (IMessageInterceptor<TRequest, TResponse>)rawInterceptor;
                    var next = pipeline;
                    pipeline = (req, ct) => interceptor.InterceptAsync(req, next, ct);
                }
            }

            // Execute pipeline
            return pipeline(request, cancellationToken);
        }

        public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) where TResponse : IResponse
        {
            var requestType = request.GetType();
            if (!_handlers.TryGetValue(requestType, out var rawHandler))
            {
                throw new InvalidOperationException(
                    $"No handler registered for {requestType.Name}"
                );
            }

            // Use reflection to invoke the generic handler
            var handlerInterface = rawHandler.GetType()
                .GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition().Name.StartsWith("ITransportHandler") &&
                    i.GenericTypeArguments.Length == 2 &&
                    typeof(IRequest<TResponse>).IsAssignableFrom(i.GenericTypeArguments[0]) &&
                    typeof(TResponse).IsAssignableFrom(i.GenericTypeArguments[1])
                );

            if (handlerInterface == null)
                throw new InvalidOperationException($"Handler for {requestType.Name} does not implement expected interface.");

            var handlerRequestType = handlerInterface.GenericTypeArguments[0];
            var handlerResponseType = handlerInterface.GenericTypeArguments[1];

            // Build pipeline dynamically
            Func<object, CancellationToken, Task<object>> pipeline = async (req, ct) =>
            {
                var sendAsyncMethod = rawHandler.GetType().GetMethod("SendAsync", new[] { handlerRequestType, typeof(CancellationToken) });
                if (sendAsyncMethod == null)
                    throw new InvalidOperationException("Handler does not have SendAsync method.");
                var task = (Task)sendAsyncMethod.Invoke(rawHandler, new object[] { req, ct });
                await task.ConfigureAwait(false);
                var resultProperty = task.GetType().GetProperty("Result");
                return resultProperty.GetValue(task);
            };

            // Wrap with interceptors if any
            if (_interceptors.TryGetValue(requestType, out var list))
            {
                foreach (var rawInterceptor in list.Cast<object>().Reverse())
                {
                    var interceptorType = typeof(IMessageInterceptor<,>).MakeGenericType(handlerRequestType, handlerResponseType);
                    var interceptAsyncMethod = interceptorType.GetMethod("InterceptAsync");
                    var next = pipeline;
                    pipeline = async (req, ct) =>
                    {
                        // nextDelegate: (TRequest, CancellationToken) => Task<TResponse>
                        Func<object, CancellationToken, Task<object>> nextDelegate = next;
                        // Create strongly-typed next
                        var stronglyTypedNext = Delegate.CreateDelegate(
                            typeof(Func<,,>).MakeGenericType(handlerRequestType, typeof(CancellationToken), typeof(Task<>).MakeGenericType(handlerResponseType)),
                            next.Target, next.Method
                        );
                        var task = (Task)interceptAsyncMethod.Invoke(rawInterceptor, new object[] { req, stronglyTypedNext, ct });
                        await task.ConfigureAwait(false);
                        var resultProperty = task.GetType().GetProperty("Result");
                        return resultProperty.GetValue(task);
                    };
                }
            }

            // Execute pipeline
            return pipeline(request, cancellationToken).ContinueWith(t => (TResponse)t.Result, cancellationToken);
        }
    }
}