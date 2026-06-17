using System.Reflection;
using CodeSpirit.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Http;

/// <summary>
/// Registers the built-in HTTP client and auto-proxies methods
/// annotated with [HttpGet], [HttpPost], etc.
///
/// Convention: annotated methods are registered as named HTTP operations.
/// </summary>
public static class HttpExtensions
{
    public static IServiceCollection AddHttp(this IServiceCollection services, IConfiguration config)
    {
        // Register IHttp with typed HttpClient
        services.AddHttpClient<CodeSpiritHttp>();
        services.AddSingleton<IHttp>(sp => sp.GetRequiredService<CodeSpiritHttp>());

        // Scan for [Http] annotated methods and register their containing types
        var targets = Assemblies.Find<object>();
        foreach (var type in targets)
        {
            var hasHttpMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(m => m.GetCustomAttribute<HttpAttribute>() is not null);

            if (hasHttpMethods)
                services.AddTransient(type);
        }

        return services;
    }
}
