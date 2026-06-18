using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CodeSpirit.Modules.Demo;

[Service(Lifetime = ServiceLifetime.Singleton)]
public class DemoInjection
{
    [Autowired]
    private ILogger<DemoInjection> _logger = null!;

    [Value("CodeSpirit:Name")]
    public string AppName { get; set; } = string.Empty;

    [Value("CodeSpirit:Version")]
    public string Version { get; set; } = string.Empty;

    [OnStartup(3000)]
    public void PrintInjectedValues()
    {
        _logger.LogInformation("[Autowired] Logger injected via field: {Type}", _logger.GetType().Name);
        _logger.LogInformation("[Value] AppName = {AppName}", AppName);
        _logger.LogInformation("[Value] Version = {Version}", Version);
    }
}
