using MicroComms.Core.Abstractions;
using MicroComms.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MicroComms.DependencyInjection;

public class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds MicroComms services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMicroComms(this IServiceCollection services,
        Action<MicroCommsOptions>? configureOptions = null)
    {
        var options = new MicroCommsOptions();
        configureOptions?.Invoke(options);
        if (options.Serializer == null)
        {
            throw new ArgumentNullException(nameof(options.Serializer), "Serializer cannot be null.");
        }
        if (options.Transport == null)
        {
            throw new ArgumentNullException(nameof(options.Transport), "Transport cannot be null.");
        }
        // Register the serializer and transport
        services.AddSingleton(options.Serializer);
        services.AddSingleton(options.Transport);
        // Register the message bus
        services.AddSingleton<IMessageBus, MessageBus>(provider =>
            new MessageBus(options));
        return services;
    }
}