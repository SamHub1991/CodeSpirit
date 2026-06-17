using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Plugin;

[ConditionalOnClass(typeof(CodeSpirit.Core.Plugin.IPlugin))]
public class PluginModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IPluginManager, PluginLoader>();
        context.Services.AddSingleton<PluginLoader>();
    }
}
