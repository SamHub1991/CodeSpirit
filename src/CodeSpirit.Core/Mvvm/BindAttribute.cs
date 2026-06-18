namespace CodeSpirit.Core.Mvvm;

[AttributeUsage(AttributeTargets.Property)]
public class BindAttribute : Attribute
{
    public string? Name { get; set; }
    public BindDirection Direction { get; set; } = BindDirection.OneWay;

    public BindAttribute() { }

    public BindAttribute(BindDirection direction) => Direction = direction;
}
