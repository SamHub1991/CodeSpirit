namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Marks a class as an auto-configuration component that conditionally registers services.
/// Evaluated during application startup as part of the module system.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [AutoConfiguration(ConditionalOnClass = "StackExchange.Redis.ConnectionMultiplexer")]
/// public class RedisAutoConfiguration { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class AutoConfigurationAttribute : Attribute
{
    /// <summary>
    /// Optional bean type that must exist for this configuration to activate.
    /// </summary>
    public Type? ConditionalOnBean { get; set; }

    /// <summary>
    /// Optional class name that must be available for this configuration to activate.
    /// </summary>
    public string? ConditionalOnClass { get; set; }
}
