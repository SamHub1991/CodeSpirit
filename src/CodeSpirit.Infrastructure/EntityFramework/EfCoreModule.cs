using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.EntityFramework;

[ConditionalOnClass(typeof(Microsoft.EntityFrameworkCore.DbContext))]
public class EfCoreModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddCodeSpiritEntityFrameworkCore();
    }
}
