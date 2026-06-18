namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Registers a class as a service in the DI container via source generation.
/// By default uses <see cref="ServiceLifetime.Scoped"/> lifetime.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Service]                                  // Scoped
/// [Service(Lifetime = ServiceLifetime.Singleton)]
/// [Service(ServiceType = typeof(IMyService))]  // Register as interface
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ServiceAttribute : Attribute
{
    /// <summary>
    /// DI lifetime. Default is <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Optional interface or base type to register as. If not set, the class type itself is used.
    /// </summary>
    public Type? ServiceType { get; set; }
}

/// <summary>
/// Specifies the lifetime of a service registered via <see cref="ServiceAttribute"/>.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>One instance per application.</summary>
    Singleton,
    /// <summary>One instance per scope/request.</summary>
    Scoped,
    /// <summary>New instance each time.</summary>
    Transient
}
