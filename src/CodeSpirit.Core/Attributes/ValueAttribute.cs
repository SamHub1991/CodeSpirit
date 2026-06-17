namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ValueAttribute : Attribute
{
    public string Key { get; }

    public ValueAttribute(string key)
    {
        Key = key;
    }
}
