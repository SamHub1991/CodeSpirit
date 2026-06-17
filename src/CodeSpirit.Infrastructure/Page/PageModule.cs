using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Page;

[ConditionalOnClass(typeof(CodeSpirit.Core.Page.PageDirectiveAttribute))]
public class PageModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<PageParser>();
        context.Services.AddSingleton<PageRenderer>();
    }
}
