using Castle.DynamicProxy;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace CodeSpirit.Infrastructure.Aop;

public static class AopExtensions
{
    private static readonly ConcurrentDictionary<Type, bool> _aopProxyCache = new();

    public static IServiceCollection AddCodeSpiritAop(this IServiceCollection services)
    {
        services.AddSingleton<ProxyGenerator>();
        services.AddSingleton<TransactionInterceptor>();
        services.AddSingleton<CacheInterceptor>();
        services.AddSingleton<CodeSpiritInterceptorSelector>();

        return services;
    }

    public static bool NeedsAopProxy(Type type) =>
        _aopProxyCache.GetOrAdd(type, static t =>
        {
            foreach (var method in t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.DeclaringType == typeof(object)) continue;

                if (method.GetCustomAttribute<TransactionalAttribute>() != null ||
                    method.GetCustomAttribute<CacheableAttribute>() != null)
                    return true;
            }

            return false;
        });

    public static object CreateProxy(IServiceProvider sp, Type type, object target)
    {
        var generator = sp.GetRequiredService<ProxyGenerator>();
        var selector = sp.GetRequiredService<CodeSpiritInterceptorSelector>();
        var interceptors = new IInterceptor[]
        {
            sp.GetRequiredService<TransactionInterceptor>(),
            sp.GetRequiredService<CacheInterceptor>()
        };

        var options = new ProxyGenerationOptions { Selector = selector };
        return generator.CreateClassProxyWithTarget(type, target, options, interceptors);
    }
}

public class CodeSpiritInterceptorSelector : IInterceptorSelector
{
    private static readonly ConcurrentDictionary<MethodInfo, bool> _txCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, bool> _cacheCache = new();

    public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
    {
        var result = new List<IInterceptor>();

        var needsTx = _txCache.GetOrAdd(method, static m =>
            m.GetCustomAttribute<TransactionalAttribute>() != null);

        if (needsTx)
        {
            var tx = interceptors.OfType<TransactionInterceptor>().FirstOrDefault();
            if (tx != null) result.Add(tx);
        }

        var needsCache = _cacheCache.GetOrAdd(method, static m =>
            m.GetCustomAttribute<CacheableAttribute>() != null);

        if (needsCache)
        {
            var cache = interceptors.OfType<CacheInterceptor>().FirstOrDefault();
            if (cache != null) result.Add(cache);
        }

        return result.ToArray();
    }
}
