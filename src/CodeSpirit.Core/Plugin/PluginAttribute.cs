namespace CodeSpirit.Core.Plugin;

[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; init; } = "1.0.0";
    public string Description { get; init; } = string.Empty;

    public PluginAttribute(string name) => Name = name;
}