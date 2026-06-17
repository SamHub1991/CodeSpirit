using System.Reflection;

namespace CodeSpirit.Core.Interfaces;

public interface IModuleLoader
{
    List<Type> ResolveModuleTopology(Assembly assembly);
}
