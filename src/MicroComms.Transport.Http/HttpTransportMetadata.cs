using MicroComms.Core.Abstractions;

namespace MicroComms.Transport.Http;

public sealed class HttpTransportMetadata : ITransportMetadata
{
    public string Route { get; set; } = default!;
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public Type ResponseType { get; set; } = default!;
    public string BaseAddress { get; set; } = default!;
}
