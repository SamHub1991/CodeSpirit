namespace CodeSpirit.Core.Mvvm;

[AttributeUsage(AttributeTargets.Property)]
public class FromRouteAttribute : Attribute
{
    public string? Name { get; }

    public FromRouteAttribute() { }

    public FromRouteAttribute(string name) => Name = name;
}