using MicroComms.Core.Abstractions;
using System.Text.Json;

namespace MicroComms.Serialization.Json;

public class JsonSerializerAdapter(JsonSerializerOptions? options = null) : ISerializer
{
    private readonly JsonSerializerOptions _options = options ?? JsonSerializerOptions.Web;

    public byte[] Serialize<T>(T obj)
        => JsonSerializer.SerializeToUtf8Bytes(obj, _options);

    public T Deserialize<T>(byte[] data)
        => JsonSerializer.Deserialize<T>(data, _options)!;

    public object Deserialize(Type type, byte[] data)
    => JsonSerializer.Deserialize(data, type, _options)!;
}