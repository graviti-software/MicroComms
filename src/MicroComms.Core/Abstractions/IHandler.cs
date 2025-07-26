using MicroComms.Core.Models;

namespace MicroComms.Core.Abstractions;

public interface IHandler
{
    /// <summary>Fire-and-forget.</summary>
    Task SendAsync<T>(T message, CancellationToken cancellationToken = default);

    /// <summary>Request/response via correlation.</summary>
    Task<Response> RequestAsync<TRequest>(
        TRequest message,
        CancellationToken cancellationToken = default
    ) where TRequest : IRequest;

    /// <summary>Request/response with payload.</summary>
    Task<Response<TPayload>> RequestAsync<TRequest, TPayload>(
        TRequest message,
        CancellationToken cancellationToken = default
    ) where TRequest : IRequest<TPayload>;
}