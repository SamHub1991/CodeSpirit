namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AutoConfigurationAttribute : Attribute
{
    public Type? ConditionalOnBean { get; set; }
    public string? ConditionalOnClass { get; set; }
}
