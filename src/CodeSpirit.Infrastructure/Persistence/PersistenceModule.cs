using CodeSpirit.Core.Abstractions;
using CodeSpirit.Infrastructure.EntityFramework;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Persistence;

public class PersistenceModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddCodeSpiritEntityFrameworkCore();
    }
}
