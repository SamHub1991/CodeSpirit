namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Marks a class as a data repository with automatic CRUD registration.
/// Typically used with Entity Framework DbContext classes.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Repository]
/// public class UserRepository(UserDbContext db) { ... }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class RepositoryAttribute : Attribute
{
}
