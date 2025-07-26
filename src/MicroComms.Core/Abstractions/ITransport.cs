namespace MicroComms.Core.Abstractions;

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
