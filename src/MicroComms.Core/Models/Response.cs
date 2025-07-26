namespace MicroComms.Core.Models;

/// <summary>
/// Correlation ACK sent by a receiver to acknowledge or reject a message.
/// </summary>
public record Response(Guid CorrelationId,
    int StatusCode,
    string? ErrorMessage = null);

public record Response<TPayload>(Guid CorrelationId,
    int StatusCode,
    TPayload? Payload = default,
    string? ErrorMessage = null)
    : Response(CorrelationId, StatusCode, ErrorMessage);