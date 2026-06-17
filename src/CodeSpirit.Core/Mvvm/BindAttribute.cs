namespace CodeSpirit.Core.Mvvm;

[AttributeUsage(AttributeTargets.Property)]
public class BindAttribute : Attribute
{
    public string? Direction { get; set; }
}