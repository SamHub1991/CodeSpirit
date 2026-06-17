namespace CodeSpirit.Core;

/// <summary>
/// Conditionally registers a component only if a specific type is available.
/// Use typeof() for compile-time safety.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RequireAttribute : Attribute
{
    public Type? Type { get; }
    public string? TypeName { get; }

    public RequireAttribute(Type type)
    {
        Type = type;
        TypeName = type.FullName;
    }

    public RequireAttribute(string typeName)
    {
        TypeName = typeName;
    }

    public bool IsSatisfied => Type is not null
        ? Type.Assembly.GetType(TypeName!) is not null
        : Type.GetType(TypeName!) is not null;
}

/// <summary>
/// Conditionally registers a component only if a configuration property exists and has a specific value.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RequireConfigAttribute : Attribute
{
    public string Key { get; }
    public string? Value { get; set; }

    public RequireConfigAttribute(string key)
    {
        Key = key;
    }
}
