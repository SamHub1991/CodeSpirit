using System.Reflection;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.AutoConfiguration;

public static class AttributeInjectionExtensions
{
    public static IServiceCollection AddCodeSpiritAttributeInjection(this IServiceCollection services)
    {
        for (int i = 0; i < services.Count; i++)
        {
            var desc = services[i];
            var implType = desc.ImplementationType;
            if (implType == null || implType.IsGenericTypeDefinition) continue;

            if (!HasCodeSpiritAttribute(implType)) continue;

            services[i] = ServiceDescriptor.Describe(
                desc.ServiceType,
                sp =>
                {
                    var instance = ActivatorUtilities.CreateInstance(sp, implType);
                    InjectAutowired(sp, instance);
                    InjectValues(sp, instance);
                    return instance;
                },
                desc.Lifetime
            );
        }

        return services;
    }

    private static bool HasCodeSpiritAttribute(Type type)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<AutowiredAttribute>() != null) return true;
            if (prop.GetCustomAttribute<ValueAttribute>() != null) return true;
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.GetCustomAttribute<AutowiredAttribute>() != null) return true;
        }

        return false;
    }

    private static void InjectAutowired(IServiceProvider sp, object instance)
    {
        var type = instance.GetType();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        foreach (var prop in type.GetProperties(flags))
        {
            if (prop.GetCustomAttribute<AutowiredAttribute>() == null) continue;
            var dep = sp.GetService(prop.PropertyType);
            if (dep != null && prop.CanWrite)
                prop.SetValue(instance, dep);
        }

        foreach (var field in type.GetFields(flags))
        {
            if (field.GetCustomAttribute<AutowiredAttribute>() == null) continue;
            var dep = sp.GetService(field.FieldType);
            if (dep != null)
                field.SetValue(instance, dep);
        }
    }

    private static void InjectValues(IServiceProvider sp, object instance)
    {
        var config = sp.GetService<IConfiguration>();
        if (config == null) return;

        var type = instance.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<ValueAttribute>();
            if (attr == null) continue;

            var value = config[attr.Key];
            if (value != null && prop.CanWrite)
            {
                var converted = ConvertValue(value, prop.PropertyType);
                prop.SetValue(instance, converted);
            }
        }
    }

    private static object? ConvertValue(string value, Type target)
    {
        var underlying = Nullable.GetUnderlyingType(target) ?? target;
        try
        {
            return underlying switch
            {
                Type t when t == typeof(int) => int.Parse(value),
                Type t when t == typeof(long) => long.Parse(value),
                Type t when t == typeof(double) => double.Parse(value),
                Type t when t == typeof(bool) => bool.Parse(value),
                Type t when t == typeof(Guid) => Guid.Parse(value),
                Type t when t == typeof(TimeSpan) => TimeSpan.Parse(value),
                Type t when t == typeof(DateTime) => DateTime.Parse(value),
                _ => Convert.ChangeType(value, underlying)
            };
        }
        catch
        {
            return value;
        }
    }
}
