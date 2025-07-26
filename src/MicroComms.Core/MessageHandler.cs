using MicroComms.Core.Abstractions;
using MicroComms.Core.Models;

namespace MicroComms.Core;

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