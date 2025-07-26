namespace MicroComms.Core.Models;

public record MessageFrame(Guid Id, string Type, byte[] Payload);