using MicroComms.Core.Abstractions;
using System.Text.Json;

namespace MicroComms.Serialization.Adapters;

public class JsonSerializerAdapter : ISerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializerAdapter(JsonSerializerOptions? options = null)
    {
        _options = options
            ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    public byte[] Serialize<T>(T obj)
        => JsonSerializer.SerializeToUtf8Bytes(obj, _options);

    public T Deserialize<T>(byte[] data)
        => JsonSerializer.Deserialize<T>(data, _options)!;

    public object Deserialize(Type type, byte[] data)
    => JsonSerializer.Deserialize(data, type, _options)!;
}