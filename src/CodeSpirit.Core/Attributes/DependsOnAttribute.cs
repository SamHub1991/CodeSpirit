namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    public Type[] ModuleTypes { get; }

    public DependsOnAttribute(params Type[] moduleTypes)
    {
        ModuleTypes = moduleTypes;
    }
}
