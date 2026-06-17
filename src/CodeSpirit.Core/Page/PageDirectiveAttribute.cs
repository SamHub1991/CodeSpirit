namespace CodeSpirit.Core.Page;

[AttributeUsage(AttributeTargets.Class)]
public class PageDirectiveAttribute : Attribute
{
    public string? Route { get; set; }
    public string? ViewModelType { get; set; }
    public string? Layout { get; set; }
    public string? Title { get; set; }
}