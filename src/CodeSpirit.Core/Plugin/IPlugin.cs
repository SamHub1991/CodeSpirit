using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Core.Plugin;

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    bool IsEnabled { get; }

    Task InstallAsync(IServiceCollection services);
    Task UninstallAsync();
    Task StartAsync();
    Task StopAsync();
}

public interface IPluginManager
{
    IReadOnlyList<IPlugin> LoadedPlugins { get; }

    Task<IPlugin> LoadPluginAsync(string assemblyPath);
    Task UnloadPluginAsync(string pluginName);
    Task EnablePluginAsync(string pluginName);
    Task DisablePluginAsync(string pluginName);
}