namespace CodeSpirit.Core.Page;

/// <summary>
/// Marks a method to be invoked before the ViewModel's <c>LoadAsync</c> method.
/// Use for pre-load setup such as authentication checks or context initialization.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [BeforeLoad]
/// public void CheckAuthorization() { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class BeforeLoadAttribute : Attribute { }

/// <summary>
/// Marks a method to be invoked after the ViewModel's <c>LoadAsync</c> method.
/// Use for post-load processing such as enrichment or validation.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [AfterLoad]
/// public void EnrichData() { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class AfterLoadAttribute : Attribute { }