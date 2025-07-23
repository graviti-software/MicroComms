namespace MicroComms.Client.Models;

public class MessageFrame
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public byte[] Payload { get; set; } = null!;
}