using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CodeSpirit.Core.Interfaces;

public interface IAutoServiceRegistrar
{
    void RegisterServices(IServiceCollection services, Assembly assembly, IConfiguration? configuration = null);
}
