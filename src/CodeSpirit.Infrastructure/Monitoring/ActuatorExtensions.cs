using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Infrastructure.Monitoring;

public static class ActuatorExtensions
{
    public static IServiceCollection AddCodeSpiritActuator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks();
        services.AddMemoryCache();
        return services;
    }

    public static WebApplication UseCodeSpiritActuatorEndpoints(this WebApplication app)
    {
        app.MapGet("/actuator/health", async (HealthCheckService healthCheckService) =>
        {
            var report = await healthCheckService.CheckHealthAsync();
            var result = new
            {
                status = report.Status.ToString(),
                components = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new
                    {
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        data = e.Value.Data
                    }
                )
            };
            return Results.Ok(result);
        });

        app.MapGet("/actuator/metrics", () =>
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var result = new
            {
                memory = new
                {
                    workingSet = process.WorkingSet64,
                    privateMemory = process.PrivateMemorySize64,
                    managed = GC.GetTotalMemory(false)
                },
                threads = process.Threads.Count,
                uptime = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).ToString(),
                cpuTime = process.TotalProcessorTime.ToString()
            };
            return Results.Ok(result);
        });

        app.MapGet("/actuator/info", (IConfiguration config) =>
        {
            var result = new
            {
                application = config["CodeSpirit:Name"] ?? "CodeSpirit Application",
                version = config["CodeSpirit:Version"] ?? "1.0.0",
                framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                os = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}"
            };
            return Results.Ok(result);
        });

        return app;
    }
}
