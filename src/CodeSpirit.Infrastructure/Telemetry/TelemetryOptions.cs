namespace CodeSpirit.Infrastructure.Telemetry;

public record TelemetryOptions
{
    public string ServiceName { get; init; } = "CodeSpirit";
    public string ServiceVersion { get; init; } = "1.0";
    public string? OtlpEndpoint { get; init; }
    public bool EnableConsoleExporter { get; init; } = true;
    public bool EnableTracing { get; init; } = true;
    public bool EnableMetrics { get; init; } = true;
}
