namespace CodeSpirit.Core.Attributes;

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

    public bool IsSatisfied()
    {
        if (ClassType is not null)
            return Type.GetType(ClassType.FullName!) is not null;
        if (ClassName is not null)
            return Type.GetType(ClassName) is not null;
        return false;
    }
}
