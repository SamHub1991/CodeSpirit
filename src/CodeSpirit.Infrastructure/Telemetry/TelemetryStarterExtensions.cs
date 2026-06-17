using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CodeSpirit.Infrastructure.Telemetry;

public static class TelemetryStarterExtensions
{
    public static IServiceCollection AddCodeSpiritTelemetry(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = new TelemetryOptions();
        configuration.GetSection("Telemetry").Bind(options);

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: options.ServiceName,
                serviceVersion: options.ServiceVersion));

        if (options.EnableTracing)
        {
            builder.WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (!string.IsNullOrEmpty(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(o =>
                        o.Endpoint = new Uri(options.OtlpEndpoint));
                }

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }
            });
        }

        if (options.EnableMetrics)
        {
            builder.WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });
        }

        return services;
    }
}
