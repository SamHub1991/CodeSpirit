using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Core.Abstractions;

public class ServiceConfigurationContext
{
    public IServiceCollection Services { get; }
    public IConfiguration Configuration { get; }

    public ServiceConfigurationContext(IServiceCollection services, IConfiguration configuration)
    {
        Services = services;
        Configuration = configuration;
    }
}
