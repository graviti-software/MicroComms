using MicroComms.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MicroComms.Core;

/// <summary>
/// Default mediator implementation that uses TransportRegistry.
/// </summary>
public class RequestMediator(IServiceProvider sp) : IRequestMediator
{
    private readonly IServiceProvider _sp = sp;

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) where TResponse : IResponse
    {
        var requestType = request.GetType();

        // 1) Resolve the core handler using reflection
        var handlerType = typeof(ITransportHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        dynamic handler = _sp.GetRequiredService(handlerType);

        // 2) Grab any registered interceptors using reflection
        var interceptorType = typeof(IMessageInterceptor<,>).MakeGenericType(requestType, typeof(TResponse));
        var interceptors = _sp
            .GetServices(interceptorType)
            .Cast<dynamic>()
            .Reverse()
            .ToArray();

        // 3) Build the pipeline using dynamic
        Func<object, CancellationToken, Task<TResponse>> pipeline = (r, ct) => handler.SendAsync((dynamic)r, ct);

        foreach (var interceptor in interceptors)
        {
            var next = pipeline;
            pipeline = (r, ct) => interceptor.InterceptAsync((dynamic)r, next, ct);
        }

        // 4) Invoke the pipeline
        return await pipeline(request, cancellationToken);
    }
}