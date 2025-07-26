using MicroComms.Core;
using MicroComms.Core.Abstractions;

namespace MicroComms.Models;

public class MicroCommsOptions
{
    /// <summary>
    /// The default serializer to use for message payloads.
    /// </summary>
    public ISerializer? Serializer { get; set; }
    /// <summary>
    /// The default transport to use for sending messages.
    /// </summary>
    public ITransport? Transport { get; set; }
    /// <summary>
    /// The delay in milliseconds before attempting to reconnect after a disconnection.
    /// </summary>
    public int ReconnectDelay { get; set; } = 5000;
}