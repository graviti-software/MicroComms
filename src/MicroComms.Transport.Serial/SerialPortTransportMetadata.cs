using MicroComms.Core.Abstractions;
using System.IO.Ports;

namespace MicroComms.Transport.Serial;

public sealed class SerialPortTransportMetadata : ITransportMetadata
{
    public string PortName { get; set; } = default!;
    public int BaudRate { get; set; } = 9600;
    public Parity Parity { get; set; } = Parity.None;
    public int DataBits { get; set; } = 8;
    public StopBits StopBits { get; set; } = StopBits.One;
}
