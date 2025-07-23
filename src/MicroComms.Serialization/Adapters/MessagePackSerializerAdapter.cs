using MessagePack;
using MicroComms.Core.Abstractions;

namespace MicroComms.Serialization.Adapters;

public class MessagePackSerializerAdapter : ISerializer
{
    public byte[] Serialize<T>(T obj)
        => MessagePackSerializer.Serialize(obj);

    public T Deserialize<T>(byte[] data)
        => MessagePackSerializer.Deserialize<T>(data);

    public object Deserialize(Type type, byte[] data)
    => MessagePackSerializer.Deserialize(type, data)!;
}