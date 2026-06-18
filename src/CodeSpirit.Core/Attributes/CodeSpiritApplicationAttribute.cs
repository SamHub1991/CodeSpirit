namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Marks a Program class as a CodeSpirit application entry point.
/// Enables module scanning, auto-configuration, and service discovery.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [CodeSpiritApplication]
/// public class Program
/// {
///     public static void Main(string[] args)
///     {
///         var builder = WebApplication.CreateBuilder(args);
///         builder.AddCodeSpirit();
///         var app = builder.Build();
///         app.UseCodeSpirit();
///         app.Run();
///     }
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class CodeSpiritApplicationAttribute : Attribute
{
}
