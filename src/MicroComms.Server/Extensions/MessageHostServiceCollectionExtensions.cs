using MicroComms.Core.Abstractions;
using MicroComms.Serialization.Json;
using Microsoft.Extensions.DependencyInjection;

namespace MicroComms.Server.Extensions;

public class MessageHostOptions
{
    /// <summary>
    /// The request path (e.g. "/ws") where WebSocket connections will be accepted.
    /// </summary>
    public string Path { get; set; } = "/ws";

    /// <summary>
    /// Serializer to use for payloads.
    /// </summary>
    public ISerializer Serializer { get; set; } = new JsonSerializerAdapter();
}

public static class MessageHostServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MessageHost as the IMessageBus and configures its options.
    /// </summary>
    public static IServiceCollection AddMessageHost(this IServiceCollection services, Action<MessageHostOptions> configure)
    {
        // 1) Bind options from the delegate
        services.Configure(configure);

        // 2) Register IMessageBus → MessageHost (but do not start it yet)
        //services.AddSingleton<IMessageBus>(sp =>
        //{
        //    var opts = sp.GetRequiredService<IOptions<MessageHostOptions>>().Value;
        //    // We pass the Path into the URL prefix; actual listening will be wired in middleware
        //    return new MessageHost(
        //        urlPrefix: opts.Path,
        //        serializer: opts.Serializer
        //    );
        //});

        return services;
    }
}