namespace CodeSpirit.Infrastructure.Monitoring;

public class ActuatorOptions
{
    public bool EnableHealthEndpoint { get; set; } = true;
    public bool EnableMetricsEndpoint { get; set; } = true;
    public bool EnableInfoEndpoint { get; set; } = true;
    public long MemoryThresholdBytes { get; set; } = 1024L * 1024 * 1024; // 1 GB
}
