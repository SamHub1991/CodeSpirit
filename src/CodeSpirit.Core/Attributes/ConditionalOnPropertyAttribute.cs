namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Conditionally registers a component only when a configuration property has a specific value.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [ConditionalOnProperty("Redis:Enabled", HavingValue = "true")]
/// public class RedisModule : CodeSpiritModule { ... }
/// </code>
/// </remarks>
[Obsolete("Use RequireConfigAttribute instead")]
[AttributeUsage(AttributeTargets.Class)]
public class ConditionalOnPropertyAttribute : Attribute
{
    /// <summary>
    /// Configuration key to check.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Expected value. If null, checks only that the property exists.
    /// </summary>
    public string? HavingValue { get; set; }

    /// <summary>
    /// If true, matches when the property is missing.
    /// </summary>
    public bool MatchIfMissing { get; set; }

    public ConditionalOnPropertyAttribute(string name)
    {
        Name = name;
    }
}
