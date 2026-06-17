using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Modules.Demo.Jobs;

/// <summary>
/// Demonstrates [Scheduled], [Every], and [OnStartup] annotations.
/// Convention: method name = job name.
/// </summary>
[Service(Lifetime = ServiceLifetime.Transient)]
public class DemoJobs
{
    private readonly IHttp _http;
    private readonly ILogger<DemoJobs> _logger;

    public DemoJobs(IHttp http, ILogger<DemoJobs> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Runs every 5 minutes (cron expression).
    /// </summary>
    [Scheduled("0 */5 * * * ?", Name = "DataSync")]
    public async Task SyncDataAsync()
    {
        _logger.LogInformation("[DataSync] Syncing data at {Time}", DateTime.UtcNow);
        // Use built-in HTTP client to fetch remote data
        var data = await _http.Get<object>("https://httpbin.org/get");
        _logger.LogInformation("[DataSync] Received data: {Data}", data);
    }

    /// <summary>
    /// Runs every 60 seconds (simple interval).
    /// </summary>
    [Every(60, Name = "HealthPing")]
    public async Task PingHealthAsync()
    {
        _logger.LogInformation("[HealthPing] Pinging at {Time}", DateTime.UtcNow);
    }

    /// <summary>
    /// Runs once on startup, with 2 second delay.
    /// </summary>
    [OnStartup(2000)]
    public async Task WarmCacheAsync()
    {
        _logger.LogInformation("[WarmCache] Warming cache on startup");
    }
}
