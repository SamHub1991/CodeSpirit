namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Marks a property or field for automatic dependency injection.
/// The DI container resolves and sets the value after the object is constructed.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Autowired]
/// private ILogger&lt;MyService&gt; _logger = null!;
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class AutowiredAttribute : Attribute
{
}
