namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Declares module dependencies. The current module will only load after specified modules.
/// The framework resolves load order and detects circular dependencies.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [DependsOn(typeof(CachingModule), typeof(LoggingModule))]
/// public class MyModule : CodeSpiritModule { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    /// <summary>
    /// Module types that must be loaded before this module.
    /// </summary>
    public Type[] ModuleTypes { get; }

    public DependsOnAttribute(params Type[] moduleTypes)
    {
        ModuleTypes = moduleTypes;
    }
}
