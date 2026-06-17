namespace CodeSpirit.Core.Mvvm;

[AttributeUsage(AttributeTargets.Property)]
public class FromQueryAttribute : Attribute
{
    public string? Name { get; }

    public FromQueryAttribute() { }

    public FromQueryAttribute(string name) => Name = name;
}