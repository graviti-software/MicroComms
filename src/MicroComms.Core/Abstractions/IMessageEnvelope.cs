namespace MicroComms.Core.Abstractions;

/// <summary>
/// Wraps any payload for transport.
/// </summary>
public interface IMessageEnvelope
{
    Guid Id { get; }
    string Type { get; }
    byte[] Payload { get; }
}

public interface IRequest
{
    string Destination { get; }
}

#pragma warning disable S2326 // Unused type parameters should be removed

public interface IRequest<TResponse> : IRequest;

#pragma warning restore S2326 // Unused type parameters should be removed

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Handle a request and return a response.</summary>
    Task<TResponse> HandleAsync(TRequest request);
}

public interface IRequestHandler<in TRequest>
    where TRequest : IRequest
{
    /// <summary>Handle a request and return a response.</summary>
    Task HandleAsync(TRequest request);
}