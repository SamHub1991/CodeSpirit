namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Conditionally registers a component only when a specified type or class is available at runtime.
/// Use <c>typeof()</c> overload for compile-time safety.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [ConditionalOnClass(typeof(StackExchange.Redis.ConnectionMultiplexer))]
/// public class RedisCacheModule : CodeSpiritModule { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class ConditionalOnClassAttribute : Attribute
{
    public Type? ClassType { get; }
    public string? ClassName { get; }

    public ConditionalOnClassAttribute(Type classType)
    {
        ClassType = classType;
        ClassName = classType.FullName;
    }

    public ConditionalOnClassAttribute(string className)
    {
        ClassName = className;
    }

    /// <summary>
    /// Checks whether the target class can be resolved at runtime.
    /// </summary>
    public bool IsSatisfied()
    {
        if (ClassType is not null)
            return Type.GetType(ClassType.FullName!) is not null;
        if (ClassName is not null)
            return Type.GetType(ClassName) is not null;
        return false;
    }
}
