using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.Core.Interfaces;

public interface ICodeSpiritGeneratedRegistrar
{
    void RegisterServices(IServiceCollection services, IConfiguration configuration);
}
