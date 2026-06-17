using CodeSpirit.Core.Interfaces;

namespace CodeSpirit.Core.Abstractions;

public abstract class CodeSpiritModule : ICodeSpiritModule
{
    public virtual void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    public virtual void Configure(IServiceProvider serviceProvider)
    {
    }
}
