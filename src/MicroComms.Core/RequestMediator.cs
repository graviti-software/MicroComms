using MicroComms.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace MicroComms.Core;

/// <summary>
/// Default mediator implementation that uses TransportRegistry.
/// </summary>
public class RequestMediator(IServiceProvider sp) : IRequestMediator
{
    private readonly IServiceProvider _sp = sp;

    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default
    ) where TRequest : IRequest<TResponse>
      where TResponse : IResponse
    {
        // 1) Resolve the core handler
        var handler = _sp.GetRequiredService<
            ITransportHandler<TRequest, TResponse>
        >();

        // 2) Grab any registered interceptors
        var interceptors = _sp
           .GetServices<IMessageInterceptor<TRequest, TResponse>>()
           .Reverse()   // so they wrap in registration order
           .ToArray();

        // 3) Build the pipeline
        Func<TRequest, CancellationToken, Task<TResponse>> pipeline =
            (r, ct) => handler.SendAsync(r, ct);

        foreach (var interceptor in interceptors)
        {
            var next = pipeline;
            pipeline = (r, ct) => interceptor.InterceptAsync(r, next, ct);
        }

        // 4) Invoke the pipeline
        return await pipeline(request, cancellationToken);
    }
}