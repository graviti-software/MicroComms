using MicroComms.Core.Abstractions;

namespace MicroComms.Models;

internal record MessageEnvelope(Guid Id, string Type, byte[] Payload) : IMessageEnvelope;