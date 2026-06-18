namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Marks a method as transactional. Auto-commits on success, rolls back on exception.
/// Works with Entity Framework or any <c>IDbContextTransaction</c> implementation.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Transactional]
/// public async Task CreateOrderAsync(Order order) { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class TransactionalAttribute : Attribute
{
}
