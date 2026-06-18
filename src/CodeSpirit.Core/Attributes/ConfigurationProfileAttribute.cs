namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Activates a class only when the specified configuration profile is active.
/// Maps to ASP.NET Core environment names (Development, Staging, Production).
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [ConfigurationProfile("Development")]
/// public class DevOnlyModule : CodeSpiritModule { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class ConfigurationProfileAttribute : Attribute
{
    /// <summary>
    /// Profile name that must match the active environment.
    /// </summary>
    public string Profile { get; }

    public ConfigurationProfileAttribute(string profile)
    {
        Profile = profile;
    }
}
