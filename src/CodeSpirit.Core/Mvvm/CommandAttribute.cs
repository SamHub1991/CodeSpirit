namespace CodeSpirit.Core.Mvvm;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string? Name { get; }

    public CommandAttribute() { }

    public CommandAttribute(string name) => Name = name;
}
