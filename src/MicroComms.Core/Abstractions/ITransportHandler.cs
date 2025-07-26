namespace MicroComms.Core.Abstractions;

public interface ITransportHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse
{
    Task<TResponse> SendAsync(TRequest request, CancellationToken cancellationToken = default);
}
