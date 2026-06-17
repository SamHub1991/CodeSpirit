namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ConditionalOnPropertyAttribute : Attribute
{
    public string Name { get; }
    public string? HavingValue { get; set; }
    public bool MatchIfMissing { get; set; }

    public ConditionalOnPropertyAttribute(string name)
    {
        Name = name;
    }
}
