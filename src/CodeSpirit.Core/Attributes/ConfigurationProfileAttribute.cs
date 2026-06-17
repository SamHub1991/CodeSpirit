namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ConfigurationProfileAttribute : Attribute
{
    public string Profile { get; }

    public ConfigurationProfileAttribute(string profile)
    {
        Profile = profile;
    }
}
