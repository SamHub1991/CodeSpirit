namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ServiceAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
    public Type? ServiceType { get; set; }
}

public enum ServiceLifetime
{
    Singleton,
    Scoped,
    Transient
}
