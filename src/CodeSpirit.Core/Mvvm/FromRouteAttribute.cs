namespace CodeSpirit.Core.Mvvm;

/// <summary>
/// Binds a route parameter to a ViewModel property on every request.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [FromRoute]
/// public int Id { get; set; }    // Binds /users/{id} to this property
/// 
/// [FromRoute("bookId")]           // Binds {bookId} route param to this property
/// public int Id { get; set; }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class FromRouteAttribute : Attribute
{
    /// <summary>
    /// Route parameter name. Defaults to the property name.
    /// </summary>
    public string? Name { get; }

    public FromRouteAttribute() { }

    public FromRouteAttribute(string name) => Name = name;
}