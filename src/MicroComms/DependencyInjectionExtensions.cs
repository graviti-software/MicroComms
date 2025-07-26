using MicroComms.Core;
using MicroComms.Core.Abstractions;
using MicroComms.Models;
using MicroComms.Serialization.Json;
using MicroComms.Serialization.MessagePack;
using MicroComms.Transport.Http;
using MicroComms.Transport.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace MicroComms;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers MicroComms with named transports, serializer, mappings, and interceptors.
    /// </summary>
    public static IServiceCollection AddMicroComms(
        this IServiceCollection services,
        Action<MicroCommsOptions> configure
    )
    {
        var opts = new MicroCommsOptions();
        configure(opts);

        // 1) Serializers
        if (opts.DefaultSerializer == "json")
            services.AddSingleton<ISerializer, JsonSerializerAdapter>();
        else
            services.AddSingleton<ISerializer, MessagePackSerializerAdapter>();

        // 2) HttpClientFactory
        services.AddHttpClient();

        // 3) Register named ITransport instances
        services.AddSingleton(sp =>
        {
            var dict = new Dictionary<string, ITransport>(StringComparer.Ordinal);
            var serializer = sp.GetRequiredService<ISerializer>();
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();

            foreach (var kv in opts.HttpTransports)
            {
                var client = httpFactory.CreateClient(kv.Key);
                dict[kv.Key] = new HttpTransport(client, kv.Value);
            }
            foreach (var kv in opts.WebSocketTransports)
            {
                dict[kv.Key] = new WebSocketTransport(kv.Value, serializer);
            }
            return dict;
        });

        // 4) Register each MessageHandler for mapped requests
        foreach (var mapping in opts.RequestMappings)
        {
            services.AddSingleton(
                typeof(ITransportHandler<,>).MakeGenericType(mapping.RequestType, mapping.ResponseType),
                sp =>
                {
                    var dict = sp.GetRequiredService<Dictionary<string, ITransport>>();
                    var transport = dict[mapping.TransportName];
                    var serializer = sp.GetRequiredService<ISerializer>();
                    var handlerType = typeof(MessageHandler<,>).MakeGenericType(
                        mapping.RequestType, mapping.ResponseType
                    );
                    return Activator.CreateInstance(handlerType, transport, serializer)!;
                }
            );
        }

        // 5) Register interceptors
        foreach (var intr in opts.InterceptorMappings)
        {
            var serviceType = typeof(IMessageInterceptor<,>)
                .MakeGenericType(intr.RequestType, intr.ResponseType);
            services.AddSingleton(serviceType, intr.InterceptorType);
        }

        // 6) Register mediator
        services.AddSingleton<IRequestMediator, RequestMediator>();

        return services;
    }
}