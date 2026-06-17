using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Infrastructure.Aop;

public class CacheInterceptor : IInterceptor
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheInterceptor> _logger;

    public CacheInterceptor(IMemoryCache cache, ILogger<CacheInterceptor> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var attr = invocation.Method.GetCustomAttribute<CacheableAttribute>();
        if (attr == null)
        {
            invocation.Proceed();
            return;
        }

        var cacheKey = BuildCacheKey(attr, invocation);

        if (_cache.TryGetValue(cacheKey, out object? cachedResult))
        {
            _logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
            invocation.ReturnValue = cachedResult;
            return;
        }

        _logger.LogInformation("Cache miss for key: {CacheKey}", cacheKey);
        invocation.Proceed();

        if (invocation.ReturnValue != null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(attr.ExpirationSeconds)
            };
            _cache.Set(cacheKey, invocation.ReturnValue, options);
        }
    }

    private static string BuildCacheKey(CacheableAttribute attr, IInvocation invocation)
    {
        if (!string.IsNullOrEmpty(attr.CacheKey))
            return attr.CacheKey;

        var args = string.Join(",", invocation.Arguments.Select(a => a?.ToString() ?? "null"));
        var targetType = invocation.TargetType?.FullName ?? "Unknown";
        return $"{targetType}.{invocation.Method.Name}({args})";
    }
}
