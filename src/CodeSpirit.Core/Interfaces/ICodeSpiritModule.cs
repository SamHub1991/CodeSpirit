using CodeSpirit.Core.Abstractions;

namespace CodeSpirit.Core.Interfaces;

public interface ICodeSpiritModule
{
    void ConfigureServices(ServiceConfigurationContext context);

    void Configure(IServiceProvider serviceProvider);
}
