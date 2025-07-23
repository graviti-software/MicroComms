using MicroComms.Core.Abstractions;

namespace MicroComms.Client.Models;

internal class MessageEnvelope : IMessageEnvelope
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public byte[] Payload { get; init; } = null!;
}