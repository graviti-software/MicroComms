using MicroComms.Core.Abstractions;

namespace MicroComms.Core.Models;

public record MessageFrame(Guid Id, string Type, byte[] Payload);

// A default implementation:
public class MessageEnvelope : IMessageEnvelope
{
    public Guid Id { get; init; }
    public string Type { get; init; }
    public byte[] Payload { get; init; }
}