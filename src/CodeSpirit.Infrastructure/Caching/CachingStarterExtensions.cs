using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Caching;

public static class CachingStarterExtensions
{
    public static IServiceCollection AddCodeSpiritCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        return services;
    }
}
