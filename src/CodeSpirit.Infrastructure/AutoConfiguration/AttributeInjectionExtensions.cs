using System.Collections.Concurrent;
using System.Reflection;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Infrastructure.Aop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.AutoConfiguration;

public static class AttributeInjectionExtensions
{
    private static readonly ConcurrentDictionary<Type, TypeInjectionMetadata> _metadataCache = new();
    public static IServiceCollection AddCodeSpiritAttributeInjection(this IServiceCollection services)
    {
        for (int i = 0; i < services.Count; i++)
        {
            var desc = services[i];
            var implType = desc.ImplementationType;
            if (implType == null || implType.IsGenericTypeDefinition) continue;

            if (!HasCodeSpiritAttribute(implType) && !AopExtensions.NeedsAopProxy(implType))
                continue;

            var needsAutowired = HasCodeSpiritAttribute(implType);
            var needsProxy = AopExtensions.NeedsAopProxy(implType);

            services[i] = ServiceDescriptor.Describe(
                desc.ServiceType,
                sp =>
                {
                    var instance = ActivatorUtilities.CreateInstance(sp, implType);

                    if (needsAutowired)
                    {
                        InjectAutowired(sp, instance);
                        InjectValues(sp, instance);
                    }

                    if (needsProxy)
                    {
                        return AopExtensions.CreateProxy(sp, implType, instance);
                    }

                    return instance;
                },
                desc.Lifetime
            );
        }

        return services;
    }

    private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static TypeInjectionMetadata GetMetadata(Type type) =>
        _metadataCache.GetOrAdd(type, key =>
        {
            var meta = new TypeInjectionMetadata();

            foreach (var prop in key.GetProperties(MemberFlags))
            {
                if (prop.GetCustomAttribute<AutowiredAttribute>() is not null)
                    meta.AutowiredProperties.Add(prop);
                if (prop.GetCustomAttribute<ValueAttribute>() is { } va)
                    meta.ValueProperties.Add(new(prop, va.Key));
            }

            foreach (var field in key.GetFields(MemberFlags))
            {
                if (field.GetCustomAttribute<AutowiredAttribute>() is not null)
                    meta.AutowiredFields.Add(field);
                if (field.GetCustomAttribute<ValueAttribute>() is { } va)
                    meta.ValueFields.Add(new(field, va.Key));
            }

            return meta;
        });

    private static bool HasCodeSpiritAttribute(Type type) =>
        GetMetadata(type).HasCodeSpiritAttribute;

    private static void InjectAutowired(IServiceProvider sp, object instance)
    {
        var meta = GetMetadata(instance.GetType());
        foreach (var prop in meta.AutowiredProperties)
        {
            if (!prop.CanWrite) continue;
            var dep = sp.GetService(prop.PropertyType);
            if (dep is not null)
                prop.SetValue(instance, dep);
        }
        foreach (var field in meta.AutowiredFields)
        {
            var dep = sp.GetService(field.FieldType);
            if (dep is not null)
                field.SetValue(instance, dep);
        }
    }

    private static void InjectValues(IServiceProvider sp, object instance)
    {
        var config = sp.GetService<IConfiguration>();
        if (config is null) return;

        var meta = GetMetadata(instance.GetType());
        foreach (var (prop, key) in meta.ValueProperties)
        {
            if (!prop.CanWrite) continue;
            var value = config[key];
            if (value is not null)
                prop.SetValue(instance, ValueConverter.ConvertValue(value, prop.PropertyType));
        }
        foreach (var (field, key) in meta.ValueFields)
        {
            var value = config[key];
            if (value is not null)
                field.SetValue(instance, ValueConverter.ConvertValue(value, field.FieldType));
        }
    }

    internal sealed class TypeInjectionMetadata
    {
        public bool HasCodeSpiritAttribute =>
            AutowiredProperties.Count > 0 || AutowiredFields.Count > 0 ||
            ValueProperties.Count > 0 || ValueFields.Count > 0;

        public List<PropertyInfo> AutowiredProperties { get; } = new();
        public List<FieldInfo> AutowiredFields { get; } = new();
        public List<(PropertyInfo Property, string Key)> ValueProperties { get; } = new();
        public List<(FieldInfo Field, string Key)> ValueFields { get; } = new();
    }
}
