using Castle.DynamicProxy;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeSpirit.Infrastructure.Aop;

public static class AopExtensions
{
    public static IServiceCollection AddCodeSpiritAop(this IServiceCollection services)
    {
        services.AddSingleton<ProxyGenerator>();
        services.AddSingleton<TransactionInterceptor>();
        services.AddSingleton<CacheInterceptor>();

        return services;
    }
}

public class CodeSpiritInterceptorSelector : IInterceptorSelector
{
    public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
    {
        var result = new List<IInterceptor>();

        if (method.GetCustomAttribute<TransactionalAttribute>() != null)
        {
            var txInterceptor = interceptors.OfType<TransactionInterceptor>().FirstOrDefault();
            if (txInterceptor != null) result.Add(txInterceptor);
        }

        if (method.GetCustomAttribute<CacheableAttribute>() != null)
        {
            var cacheInterceptor = interceptors.OfType<CacheInterceptor>().FirstOrDefault();
            if (cacheInterceptor != null) result.Add(cacheInterceptor);
        }

        return result.ToArray();
    }
}
