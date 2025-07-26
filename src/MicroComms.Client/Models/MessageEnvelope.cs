using MicroComms.Core.Abstractions;

namespace MicroComms.Client.Models;

internal record MessageEnvelope(Guid Id, string Type, byte[] Payload) : IMessageEnvelope;