using CodeSpirit.Core;
using CodeSpirit.Core.Interfaces;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeSpirit.Infrastructure.AutoConfiguration;

public class AutoServiceRegistrar : IAutoServiceRegistrar
{
    public void RegisterServices(IServiceCollection services, Assembly assembly)
    {
        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }
        catch { return; }

        foreach (var type in types.Where(t => t.IsClass && !t.IsAbstract))
        {
            if (!ShouldRegister(type)) continue;

            RegisterServiceAttribute(services, type);
            RegisterRepositoryAttribute(services, type);
        }
    }

    private static bool ShouldRegister(Type type)
    {
        var require = type.GetCustomAttribute<CodeSpirit.Core.RequireAttribute>();
        if (require is not null)
            return require.IsSatisfied;

        var requireConfig = type.GetCustomAttribute<CodeSpirit.Core.RequireConfigAttribute>();
        if (requireConfig is not null)
        {
            var config = new ConfigurationBuilder().Build();
            var value = config[requireConfig.Key];
            return requireConfig.Value is null
                ? !string.IsNullOrEmpty(value)
                : value == requireConfig.Value;
        }

        return true;
    }

    private static void RegisterServiceAttribute(IServiceCollection services, Type type)
    {
        var attr = type.GetCustomAttribute<ServiceAttribute>();
        if (attr is null) return;

        var serviceType = attr.ServiceType
            ?? type.GetInterfaces()
                .Where(i => i != typeof(ICodeSpiritModule) && i != typeof(ICodeSpiritGeneratedRegistrar))
                .FirstOrDefault()
            ?? type;

        var lifetime = attr.Lifetime switch
        {
            Core.Attributes.ServiceLifetime.Singleton => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton,
            Core.Attributes.ServiceLifetime.Scoped => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped,
            Core.Attributes.ServiceLifetime.Transient => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient,
            _ => Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped
        };

        services.Add(new ServiceDescriptor(serviceType, type, lifetime));
    }

    private static void RegisterRepositoryAttribute(IServiceCollection services, Type type)
    {
        var attr = type.GetCustomAttribute<RepositoryAttribute>();
        if (attr is null) return;

        var serviceType = type.GetInterfaces().FirstOrDefault() ?? type;
        services.AddScoped(serviceType, type);
    }
}
