namespace CodeSpirit.Core.Mvvm;

/// <summary>
/// Binds a query string parameter to a ViewModel property on every request.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [FromQuery]
/// public string? Search { get; set; }
/// 
/// [FromQuery("keyword")]    // Binds ?keyword=... to this property
/// public string? Query { get; set; }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class FromQueryAttribute : Attribute
{
    /// <summary>
    /// Query string parameter name. Defaults to the property name.
    /// </summary>
    public string? Name { get; }

    public FromQueryAttribute() { }

    public FromQueryAttribute(string name) => Name = name;
}