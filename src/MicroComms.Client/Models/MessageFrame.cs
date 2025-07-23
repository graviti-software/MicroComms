using MicroComms.Core.Abstractions;

namespace MicroComms.Client.Models;

public class MessageFrame
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public byte[] Payload { get; set; } = null!;
}

internal class MessageEnvelope : IMessageEnvelope
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public byte[] Payload { get; init; } = null!;
}