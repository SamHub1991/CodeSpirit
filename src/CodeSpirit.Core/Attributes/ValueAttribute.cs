namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Injects a configuration value into a property from appsettings.json.
/// Supports nested keys using colon separator (e.g. "CodeSpirit:Name").
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Value("CodeSpirit:Name")]
/// public string AppName { get; set; } = string.Empty;
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class ValueAttribute : Attribute
{
    /// <summary>
    /// Configuration key path using colon separator for nested sections.
    /// </summary>
    public string Key { get; }

    public ValueAttribute(string key)
    {
        Key = key;
    }
}
