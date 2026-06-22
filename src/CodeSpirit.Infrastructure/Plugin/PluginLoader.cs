using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using CodeSpirit.Core.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Infrastructure.Plugin;

public class PluginLoader : IPluginManager
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly IServiceCollection? _services;
    private readonly ConcurrentDictionary<string, IPlugin> _plugins = new();
    private readonly ConcurrentDictionary<string, AssemblyLoadContext> _contexts = new();

    public PluginLoader(ILogger<PluginLoader> logger, IServiceCollection? services = null)
    {
        _logger = logger;
        _services = services;
    }

    public IReadOnlyList<IPlugin> LoadedPlugins => _plugins.Values.ToList();

    public async Task<IPlugin> LoadPluginAsync(string assemblyPath)
    {
        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Plugin assembly not found: {assemblyPath}");

        var pluginName = Path.GetFileNameWithoutExtension(assemblyPath);
        if (_plugins.ContainsKey(pluginName))
            throw new InvalidOperationException($"Plugin '{pluginName}' is already loaded");

        var context = new AssemblyLoadContext(pluginName, isCollectible: true);
        _contexts[pluginName] = context;

        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
        var assembly = context.LoadFromStream(stream);

        var pluginType = assembly.GetTypes().FirstOrDefault(t =>
            typeof(IPlugin).IsAssignableFrom(t) &&
            !t.IsAbstract &&
            !t.IsInterface);

        if (pluginType is null)
        {
            context.Unload();
            _contexts.TryRemove(pluginName, out _);
            throw new InvalidOperationException($"No IPlugin implementation found in {assemblyPath}");
        }

        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;

        if (_services != null)
        {
            await plugin.InstallAsync(_services);
            _logger.LogInformation("Plugin services installed: {PluginName}", plugin.Name);
        }

        await plugin.StartAsync();
        _plugins[pluginName] = plugin;

        _logger.LogInformation("Plugin loaded: {PluginName} v{Version}", plugin.Name, plugin.Version);

        return plugin;
    }

    public async Task UnloadPluginAsync(string pluginName)
    {
        if (!_plugins.TryGetValue(pluginName, out var plugin))
            return;

        await plugin.StopAsync();

        if (_services != null)
            await plugin.UninstallAsync();

        _plugins.TryRemove(pluginName, out _);

        if (_contexts.TryRemove(pluginName, out var context))
        {
            context.Unload();
            _logger.LogInformation("Plugin unloaded: {PluginName}", pluginName);
        }
    }

    public async Task EnablePluginAsync(string pluginName)
    {
        if (!_plugins.TryGetValue(pluginName, out var plugin))
            return;

        await plugin.StartAsync();
        _logger.LogInformation("Plugin enabled: {PluginName}", pluginName);
    }

    public async Task DisablePluginAsync(string pluginName)
    {
        if (!_plugins.TryGetValue(pluginName, out var plugin))
            return;

        await plugin.StopAsync();
        _logger.LogInformation("Plugin disabled: {PluginName}", pluginName);
    }
}
