using MicroComms.Core.Abstractions;
using MicroComms.Core.Models;
using System.IO.Ports;

namespace MicroComms.Core;

/// <summary>
/// Marker interface for requests.
/// </summary>
public interface IRequest;

/// <summary>
/// Marker interface for requests with a specific response type.
/// </summary>
/// typeparam name="TResponse">The expected response type.</typeparam>
public interface IRequest<TResponse> : IRequest where TResponse : IResponse;

/// <summary>
/// Marker interface for responses.
/// </summary>
public interface IResponse;

/// <summary>
/// Marker base for transport-specific metadata.
/// </summary>
public abstract class TransportMetadata
{ }

public sealed class HttpTransportMetadata : TransportMetadata
{
    public string Route { get; set; } = default!;
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public Type ResponseType { get; set; } = default!;
    public string BaseAddress { get; set; } = default!;
}

public sealed class SerialPortTransportMetadata : TransportMetadata
{
    public string PortName { get; set; } = default!;
    public int BaudRate { get; set; } = 9600;
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
}

public sealed class WebSocketTransportMetadata : TransportMetadata
{
    // nothing here for now—kept for symmetry
}

/// <summary>
/// Low-level transport abstraction: sends raw bytes and returns raw bytes.
/// </summary>
public interface ITransport
{
    /// <param name="data">A serialized envelope (bytes).</param>
    /// <returns>The raw response bytes (serialized envelope).</returns>
    Task<byte[]> SendAsync(
        byte[] data,
        CancellationToken cancellationToken = default
    );
}

public interface ITransportHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse
{
    Task<TResponse> SendAsync(TRequest request, CancellationToken cancellationToken = default);
}

public class MessageHandler<TRequest, TResponse>
    : ITransportHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResponse
{
    private readonly ITransport _transport;
    private readonly ISerializer _serializer;

    public MessageHandler(ITransport transport, ISerializer serializer)
    {
        _transport = transport;
        _serializer = serializer;
    }

    public async Task<TResponse> SendAsync(
        TRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // 1) wrap request
        var envelope = new MessageEnvelope
        {
            Id = Guid.NewGuid(),
            Type = typeof(TRequest).AssemblyQualifiedName!,
            Payload = _serializer.Serialize(request)
        };

        // 2) send & receive bytes
        var data = _serializer.Serialize(envelope);
        var respData = await _transport.SendAsync(data, cancellationToken);

        // 3) unwrap response
        var respEnv = _serializer.Deserialize<MessageEnvelope>(respData);
        return _serializer.Deserialize<TResponse>(respEnv.Payload);
    }
}