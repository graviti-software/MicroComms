using MicroComms.Core.Abstractions;

namespace MicroComms.Transport.Http;

public static class HttpTransportExtensions
{
    public static ITransport CreateHttpTransport(this HttpClient httpClient)
    {
        return new HttpTransport(httpClient);
    }
}