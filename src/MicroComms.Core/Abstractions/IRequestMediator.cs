namespace MicroComms.Core.Abstractions;

/// <summary>
/// Simplified mediator interface for sending requests.
/// </summary>
public interface IRequestMediator
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
      where TResponse : IResponse;
}