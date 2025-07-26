using MicroComms.Core.Abstractions;
using MicroComms.Transport.Http;

namespace MicroComms.Models;

/// <summary>
/// Options for configuring MicroComms transports, serializers, mappings, and interceptors.
/// </summary>
public class MicroCommsOptions
{
    internal string DefaultSerializer { get; private set; } = "json";
    internal readonly Dictionary<string, HttpTransportMetadata> HttpTransports = [];
    internal readonly Dictionary<string, Uri> WebSocketTransports = [];
    internal readonly List<RequestMapping> RequestMappings = [];
    internal readonly List<InterceptorMapping> InterceptorMappings = [];

    public MicroCommsOptions UseJsonSerializer()
    {
        DefaultSerializer = "json";
        return this;
    }

    public MicroCommsOptions UseMessagePackSerializer()
    {
        DefaultSerializer = "msgpack";
        return this;
    }

    public MicroCommsOptions AddHttpTransport(
        string name,
        string baseAddress,
        string route,
        HttpMethod method
    )
    {
        HttpTransports[name] = new HttpTransportMetadata
        {
            BaseAddress = baseAddress,
            Route = route,
            Method = method
        };
        return this;
    }

    public MicroCommsOptions AddWebSocketTransport(string name, Uri endpoint)
    {
        WebSocketTransports[name] = endpoint;
        return this;
    }

    public RequestMappingBuilder<TRequest, TResponse> MapRequest<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        return new RequestMappingBuilder<TRequest, TResponse>(this);
    }

    public MicroCommsOptions AddInterceptor<TRequest, TResponse, TInterceptor>()
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
        where TInterceptor : IMessageInterceptor<TRequest, TResponse>
    {
        InterceptorMappings.Add(
            new InterceptorMapping(typeof(TRequest), typeof(TResponse), typeof(TInterceptor))
        );
        return this;
    }

    internal record RequestMapping(Type RequestType, Type ResponseType, string TransportName);
    internal record InterceptorMapping(Type RequestType, Type ResponseType, Type InterceptorType);

    public class RequestMappingBuilder<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse
    {
        private readonly MicroCommsOptions _opts;

        public RequestMappingBuilder(MicroCommsOptions opts) => _opts = opts;

        public MicroCommsOptions ToHttp(string transportName)
        {
            _opts.RequestMappings.Add(
                new RequestMapping(
                    typeof(TRequest), typeof(TResponse), transportName
                )
            );
            return _opts;
        }

        public MicroCommsOptions ToWebSocket(string transportName)
        {
            _opts.RequestMappings.Add(
                new RequestMapping(
                    typeof(TRequest), typeof(TResponse), transportName
                )
            );
            return _opts;
        }
    }
}