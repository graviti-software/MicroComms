using MicroComms.Core;
using MicroComms.Core.Abstractions;
using MicroComms.Serialization.Json;
using MicroComms.Serialization.MessagePack;
using Microsoft.Extensions.DependencyInjection;

namespace MicroComms.DependencyInjection;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds MicroComms core services and invokes your transport registrations.
    /// </summary>
    /// <param name="services">IServiceCollection to extend</param>
    /// <param name="configure">
    ///   Callback to register each request type:
    ///   registry.Register&lt;TRequest&gt;(transportInstance, metadata);
    /// </param>
    public static IServiceCollection AddMicroComms(
        this IServiceCollection services
    )
    {
        // For HttpTransport’s HttpClient usage:
        services.AddHttpClient();

        services.AddKeyedSingleton<ISerializer, JsonSerializerAdapter>("json");
        services.AddKeyedSingleton<ISerializer, MessagePackSerializerAdapter>("msgpack");

        // Register the mediator
        services.AddSingleton<IRequestMediator, RequestMediator>();

        return services;
    }
}