using MessagePack;
using MessagePack.Resolvers;
using MicroComms.Core.Abstractions;

namespace MicroComms.Serialization.MessagePack;

public class MessagePackSerializerAdapter(MessagePackSerializerOptions? options = null) : ISerializer
{
    private readonly MessagePackSerializerOptions Options =
        options ??
        MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolverAllowPrivate.Instance);

    public byte[] Serialize<T>(T obj)
        => MessagePackSerializer.Serialize(obj, Options);

    public T Deserialize<T>(byte[] data)
        => MessagePackSerializer.Deserialize<T>(data, Options);

    public object Deserialize(Type type, byte[] data)
        => MessagePackSerializer.Deserialize(type, data, Options)!;
}