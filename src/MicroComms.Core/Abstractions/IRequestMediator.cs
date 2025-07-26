namespace MicroComms.Core.Abstractions;

/// <summary>
/// Simplified mediator interface for sending requests.
/// </summary>
public interface IRequestMediator
{
    Task<TResponse> SendAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default
    ) where TRequest : IRequest<TResponse>
      where TResponse : IResponse;
}
