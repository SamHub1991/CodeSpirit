using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Messaging;

public static class MessageBusStarterExtensions
{
    public static IServiceCollection AddCodeSpiritMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMQOptions>(configuration.GetSection("RabbitMQ"));

        services.AddSingleton<IEventBus, RabbitMQEventBus>();

        return services;
    }

    public static IApplicationBuilder UseCodeSpiritMessageBus(this IApplicationBuilder app)
    {
        app.ApplicationServices.GetRequiredService<IEventBus>();

        return app;
    }
}
