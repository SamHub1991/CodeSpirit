using System.Reflection;

namespace CodeSpirit.Core;

/// <summary>
/// Discovers assemblies in the current application domain.
/// Simple, predictable, no magic.
/// </summary>
public static class Assemblies
{
    private static readonly Lazy<Assembly[]> _cached = new(FindAll);
    private static readonly Lazy<Assembly[]> _codeSpiritCached = new(ResolveCodeSpirit);

    public static Assembly[] All => _cached.Value;

    public static Assembly[] CodeSpirit => _codeSpiritCached.Value;

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
                    catch { }
                }
            }
        }
        catch { }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToArray();
    }

    private static Assembly[] ResolveCodeSpirit()
    {
        var framework = _cached.Value
            .Where(a =>
            {
                var name = a.GetName().Name;
                return name is not null
                    && name.StartsWith("CodeSpirit", StringComparison.OrdinalIgnoreCase)
                    && !name.StartsWith("CodeSpirit.SourceGenerator", StringComparison.OrdinalIgnoreCase);
            });

        var entry = Assembly.GetEntryAssembly();
        if (entry is not null)
            framework = framework.Append(entry);

        return framework.Distinct().ToArray();
    }
}
