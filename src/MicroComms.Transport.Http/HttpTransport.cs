using MicroComms.Core.Abstractions;

namespace MicroComms.Transport.Http;

public interface IHttpTransport : ITransport
{
    Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default
    );

    Task SendRequestAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default
    );
}

internal class HttpTransport(HttpClient httpClient) : IHttpTransport
{
    public bool IsConnected => throw new NotImplementedException();

    public event Func<byte[], Task> OnMessageReceived = _ => Task.CompletedTask;

    public event Action? OnConnected;

    public event Action? OnDisconnected;

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SendAsync(string destination, byte[] data, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SendRequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public abstract class HttpRequestHandler<TRequest, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public abstract HttpMethod HttpMethod { get; }
    public abstract string Endpoint { get; }

    public Task<TResponse> HandleAsync(TRequest request)
    {
        throw new NotImplementedException();
    }
}