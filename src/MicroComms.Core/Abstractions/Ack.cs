namespace MicroComms.Core.Abstractions;

/// <summary>
/// Correlation ACK sent by a receiver to acknowledge or reject a message.
/// </summary>
public class Ack
{
    /// <summary>The ID of the original message being ACK’d.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Status code (e.g. 200 = OK, 400 = Bad Request, 500 = Error).</summary>
    public int StatusCode { get; set; }

    /// <summary>Optional error details.</summary>
    public string? ErrorMessage { get; set; }
}