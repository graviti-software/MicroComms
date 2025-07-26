namespace MicroComms.Core.Abstractions;

/// <summary>
/// Wraps any payload for transport.
/// </summary>
public interface IMessageEnvelope
{
    Guid Id { get; }
    string Type { get; }
    byte[] Payload { get; }
}