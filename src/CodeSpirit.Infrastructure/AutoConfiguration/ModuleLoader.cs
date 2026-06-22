using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Interfaces;
using System.Reflection;

namespace CodeSpirit.Infrastructure.AutoConfiguration;

public class ModuleLoader : IModuleLoader
{
    public List<Type> ResolveModuleTopology(Assembly assembly)
    {
        var allModules = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ICodeSpiritModule).IsAssignableFrom(t))
            .ToList();

        var visited = new HashSet<Type>();
        var resolved = new HashSet<Type>();
        var result = new List<Type>();

        foreach (var module in allModules)
        {
            TopologicalSort(module, allModules, visited, resolved, result);
        }

        return result;
    }

    private void TopologicalSort(
        Type module,
        List<Type> allModules,
        HashSet<Type> visited,
        HashSet<Type> resolved,
        List<Type> result)
    {
        if (resolved.Contains(module)) return;

        if (visited.Contains(module))
            throw new InvalidOperationException($"Circular dependency detected for module: {module.Name}");

        visited.Add(module);

        var dependsOnAttributes = module.GetCustomAttributes<DependsOnAttribute>();
        foreach (var attr in dependsOnAttributes)
        {
            foreach (var depType in attr.ModuleTypes)
            {
                if (allModules.Contains(depType) || typeof(ICodeSpiritModule).IsAssignableFrom(depType))
                {
                    TopologicalSort(depType, allModules, visited, resolved, result);
                }
            }
        }

        resolved.Add(module);
        result.Add(module);
    }
}
