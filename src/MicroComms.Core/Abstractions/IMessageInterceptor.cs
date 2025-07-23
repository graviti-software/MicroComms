namespace MicroComms.Core.Abstractions;

/// <summary>
/// Inspect or mutate envelopes on send/receive.
/// </summary>
public interface IMessageInterceptor
{
    Task OnSendingAsync(IMessageEnvelope envelope);

    Task OnReceivedAsync(IMessageEnvelope envelope);
}
