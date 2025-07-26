using MicroComms.Core;
using System.Net.Http.Headers;

namespace MicroComms.Transport.Http;

/// <summary>
/// Pure-binary HTTP transport: sends raw bytes and returns raw bytes.
/// </summary>
public class HttpTransport(HttpClient client, HttpTransportMetadata meta) : ITransport
{
    private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly HttpTransportMetadata _meta = meta;
    private readonly string _url = BuildUrl(meta.BaseAddress, meta.Route);

    public async Task<byte[]> SendAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        // 1) Create HTTP request
        using var request = new HttpRequestMessage(_meta.Method, _url)
        {
            Content = new ByteArrayContent(data)
        };
        // 2) Indicate raw-binary payload
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        // 3) Send
        var response = await _client.SendAsync(request, cancellationToken)
                                    .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // 4) Read raw bytes
        return await response.Content
                             .ReadAsByteArrayAsync(cancellationToken)
                             .ConfigureAwait(false);
    }

    private static string BuildUrl(string baseAddress, string route)
    {
        // If the route is already an absolute URL, just return it
        if (Uri.TryCreate(route, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        // Otherwise combine base + route
        var baseUrl = baseAddress.ToString().TrimEnd('/');
        var rel = route.TrimStart('/');
        return $"{baseUrl}/{rel}";
    }
}

//public interface IHttpTransport : ITransport
//{
//    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
//        TRequest request,
//        CancellationToken cancellationToken = default
//    );

//    Task SendRequestAsync<TRequest>(
//        TRequest request,
//        CancellationToken cancellationToken = default
//    );
//}

//internal class HttpTransport(HttpClient httpClient) : IHttpTransport
//{
//    public bool IsConnected => throw new NotImplementedException();

//    public event Func<byte[], Task> OnMessageReceived = _ => Task.CompletedTask;

//    public event Action? OnConnected;

//    public event Action? OnDisconnected;

//    public Task ConnectAsync(CancellationToken cancellationToken = default)
//    {
//        return Task.CompletedTask;
//    }

//    public Task DisconnectAsync(CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task SendAsync(string destination, byte[] data, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task SendRequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }
//}

//public abstract class HttpRequestHandler<TRequest, TResponse>
//    : IRequestHandler<TRequest, TResponse>
//    where TRequest : IRequest<TResponse>
//{
//    public abstract HttpMethod HttpMethod { get; }
//    public abstract string Endpoint { get; }

//    public Task<TResponse> HandleAsync(TRequest request)
//    {
//        throw new NotImplementedException();
//    }
//}