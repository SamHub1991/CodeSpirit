namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Marks a method for declarative caching. Results are cached by key and auto-expire.
/// Uses <c>{0}</c>, <c>{1}</c> etc. as positional argument placeholders in the key template.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Cacheable(CacheKey = "orders:{0}", ExpirationSeconds = 300)]
/// public async Task&lt;Order&gt; GetByIdAsync(long id) { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class CacheableAttribute : Attribute
{
    /// <summary>
    /// Cache key template. Use <c>{0}</c>, <c>{1}</c> for method argument values.
    /// </summary>
    public string CacheKey { get; set; } = string.Empty;

    /// <summary>
    /// Cache expiration in seconds. Default is 300 (5 minutes).
    /// </summary>
    public int ExpirationSeconds { get; set; } = 300;
}
