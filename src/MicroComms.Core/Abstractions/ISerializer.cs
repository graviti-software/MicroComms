namespace MicroComms.Core.Abstractions;

/// <summary>
/// Pluggable payload serializer.
/// </summary>
public interface ISerializer
{
    byte[] Serialize<T>(T obj);

    T Deserialize<T>(byte[] data);

    /// <summary>
    /// Deserialize given raw bytes into an object of the specified type.
    /// </summary>
    object Deserialize(Type type, byte[] data);
}