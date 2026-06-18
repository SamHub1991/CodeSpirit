namespace CodeSpirit.Core.Plugin;

/// <summary>
/// Registers a class as a plugin with name, version, and description metadata.
/// Plugins are automatically discovered and loaded by the plugin system.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Plugin("PaymentGateway", Version = "2.0.0", Description = "Third-party payment integration")]
/// public class PaymentPlugin : IPlugin { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Plugin version in semver format. Default is "1.0.0".
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Human-readable plugin description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    public PluginAttribute(string name) => Name = name;
}