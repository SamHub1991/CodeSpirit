namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CacheableAttribute : Attribute
{
    public string CacheKey { get; set; } = string.Empty;
    public int ExpirationSeconds { get; set; } = 300;
}
