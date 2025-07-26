using MicroComms.Core.Abstractions;
using System.IO.Ports;

namespace MicroComms.Transport.Serial;

/// <summary>
/// Pure-binary serial-port transport: opens the port for each SendAsync call,
/// sends a 4-byte length prefix + payload, reads back the 4-byte length + response, then closes.
/// </summary>
public class SerialPortTransport(SerialPortTransportMetadata meta) : ITransport
{
    private readonly SerialPortTransportMetadata _meta = meta ?? throw new ArgumentNullException(nameof(meta));

    public async Task<byte[]> SendAsync(
        byte[] data,
        CancellationToken cancellationToken = default
    )
    {
        // 1) Open a fresh SerialPort for this call
        using var port = new SerialPort(_meta.PortName, _meta.BaudRate, _meta.Parity, _meta.DataBits, _meta.StopBits)
        {
            ReadTimeout = 5000,
            WriteTimeout = 2000
        };
        if (!port.IsOpen)
        {
            port.Open();
        }

        // 2) Write length prefix + payload
        var lengthPrefix = BitConverter.GetBytes(data.Length);
        await port.BaseStream.WriteAsync(lengthPrefix, cancellationToken);
        await port.BaseStream.WriteAsync(data, cancellationToken);

        // 3) Read 4-byte length prefix
        var lenBuf = new byte[4];
        var read = 0;
        while (read < 4)
        {
            var chunk = await port.BaseStream.ReadAsync(lenBuf.AsMemory(read, 4 - read), cancellationToken);
            if (chunk <= 0)
                throw new IOException("Failed to read response length prefix");
            read += chunk;
        }
        var responseLength = BitConverter.ToInt32(lenBuf, 0);

        // 4) Read the response payload
        var respBuf = new byte[responseLength];
        read = 0;
        while (read < responseLength)
        {
            int chunk = await port.BaseStream.ReadAsync(respBuf.AsMemory(read, responseLength - read), cancellationToken);
            if (chunk <= 0)
                throw new IOException("Failed to read full response payload");
            read += chunk;
        }

        port.Close();

        // 5) Close() happens via Dispose() on the using
        return respBuf;
    }
}