using System.Reflection;

namespace CodeSpirit.Core;

/// <summary>
/// Discovers assemblies in the current application domain.
/// Simple, predictable, no magic.
/// </summary>
public static class Assemblies
{
    private static readonly Lazy<Assembly[]> _cached = new(FindAll);

    /// <summary>
    /// All loaded assemblies in the current AppDomain.
    /// Cached on first access for performance.
    /// </summary>
    public static Assembly[] All => _cached.Value;

    /// <summary>
    /// Only CodeSpirit assemblies (names starting with "CodeSpirit").
    /// </summary>
    public static Assembly[] CodeSpirit => All
        .Where(a =>
        {
            var name = a.GetName().Name;
            return name is not null
                && name.StartsWith("CodeSpirit", StringComparison.OrdinalIgnoreCase)
                && !name.StartsWith("CodeSpirit.SourceGenerator", StringComparison.OrdinalIgnoreCase);
        })
        .ToArray();

    /// <summary>
    /// Find all types that inherit from T in the specified assemblies.
    /// If no assemblies provided, searches all CodeSpirit assemblies.
    /// </summary>
    public static Type[] Find<T>(params Assembly[]? assemblies)
    {
        var scan = (assemblies is null || assemblies.Length == 0) ? CodeSpirit : assemblies;
        return scan
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .Where(t => t.IsClass && !t.IsAbstract && typeof(T).IsAssignableFrom(t))
            .ToArray();
    }

    private static Assembly[] FindAll()
    {
        try
        {
            // Preload all DLLs in the application directory
            var entryDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? "");
            if (!string.IsNullOrEmpty(entryDir) && Directory.Exists(entryDir))
            {
                foreach (var dll in Directory.EnumerateFiles(entryDir, "*.dll"))
                {
                    try
                    {
                        var name = Path.GetFileNameWithoutExtension(dll);
                        if (!name.StartsWith("CodeSpirit.SourceGenerator", StringComparison.OrdinalIgnoreCase))
                            Assembly.Load(name);
                    }
                    catch { /* Skip problematic assemblies */ }
                }
            }
        }
        catch { /* Ignore preload errors */ }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToArray();
    }
}
