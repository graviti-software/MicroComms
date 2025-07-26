namespace MicroComms.Core.Abstractions;

/// <summary>
/// Marker interface for requests.
/// </summary>
public interface IRequest;

/// <summary>
/// Marker interface for requests with a specific response type.
/// </summary>
/// <typeparam name="TResponse">The expected response type.</typeparam>
public interface IRequest<TResponse> : IRequest where TResponse : IResponse;