namespace MicroComms.Core;

/// <summary>
/// Run custom logic before/after a request is sent (logging, metrics, retries…).
/// </summary>
public interface IMessageInterceptor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse
{
    /// <param name="request">The request being sent.</param>
    /// <param name="next">
    ///   The next step in the pipeline: ultimately calls the transport handler.
    ///   You must await it to continue.
    /// </param>
    /// <param name="cancellationToken"></param>
    Task<TResponse> InterceptAsync(
        TRequest request,
        Func<TRequest, CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken
    );
}