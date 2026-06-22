using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Infrastructure.Aop;

public class CacheInterceptor : AsyncInterceptorBase
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheInterceptor> _logger;

    private static readonly object CachedTaskMarker = new();

    public CacheInterceptor(IMemoryCache cache, ILogger<CacheInterceptor> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    protected override bool ShouldIntercept(IInvocation invocation) =>
        invocation.Method.GetCustomAttribute<CacheableAttribute>() != null;

    protected override void ExecuteSync(IInvocation invocation)
    {
        var attr = invocation.Method.GetCustomAttribute<CacheableAttribute>()!;
        var cacheKey = BuildCacheKey(attr, invocation);

        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
            invocation.ReturnValue = cached;
            return;
        }

        _logger.LogInformation("Cache miss: {CacheKey}", cacheKey);
        invocation.Proceed();

        if (invocation.ReturnValue != null)
        {
            _cache.Set(cacheKey, invocation.ReturnValue, TimeSpan.FromSeconds(attr.ExpirationSeconds));
            _logger.LogInformation("Cached: {CacheKey}", cacheKey);
        }
    }

    protected override async Task ExecuteAsync(IInvocation invocation)
    {
        var attr = invocation.Method.GetCustomAttribute<CacheableAttribute>()!;
        var cacheKey = BuildCacheKey(attr, invocation);

        if (_cache.TryGetValue(cacheKey, out _))
        {
            _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
            invocation.ReturnValue = Task.CompletedTask;
            return;
        }

        _logger.LogInformation("Cache miss: {CacheKey}", cacheKey);
        invocation.Proceed();
        await (Task)invocation.ReturnValue!;
        _cache.Set(cacheKey, CachedTaskMarker, TimeSpan.FromSeconds(attr.ExpirationSeconds));
        _logger.LogInformation("Cached: {CacheKey}", cacheKey);
    }

    protected override async Task<T> ExecuteAsyncWithResult<T>(IInvocation invocation)
    {
        var attr = invocation.Method.GetCustomAttribute<CacheableAttribute>()!;
        var cacheKey = BuildCacheKey(attr, invocation);

        if (_cache.TryGetValue(cacheKey, out object? cached))
        {
            _logger.LogInformation("Cache hit: {CacheKey}", cacheKey);
            return (T)cached!;
        }

        _logger.LogInformation("Cache miss: {CacheKey}", cacheKey);
        invocation.Proceed();
        var result = await (Task<T>)invocation.ReturnValue!;

        if (result != null)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromSeconds(attr.ExpirationSeconds));
            _logger.LogInformation("Cached: {CacheKey}", cacheKey);
        }

        return result;
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
