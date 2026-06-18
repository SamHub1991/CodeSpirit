using System.Diagnostics;
using System.Runtime.InteropServices;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CodeSpirit.Infrastructure.Monitoring;

public static class ActuatorExtensions
{
    public static IServiceCollection AddCodeSpiritActuator(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        var healthBuilder = services.AddHealthChecks();

        RegisterDatabaseHealthCheck(healthBuilder, configuration);
        RegisterRedisHealthCheck(healthBuilder, configuration);
        RegisterMemoryHealthCheck(healthBuilder);

        services.Configure<ActuatorOptions>(configuration.GetSection("CodeSpirit:Actuator"));

        return services;
    }

    public static WebApplication UseCodeSpiritActuatorEndpoints(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ActuatorOptions>>().Value;

        if (options.EnableHealthEndpoint)
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
        }

        if (options.EnableMetricsEndpoint)
        {
            app.MapGet("/actuator/metrics", () =>
            {
                var process = Process.GetCurrentProcess();
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
        }

        if (options.EnableInfoEndpoint)
        {
            app.MapGet("/actuator/info", (IConfiguration config) =>
            {
                var result = new
                {
                    application = config["CodeSpirit:Name"] ?? "CodeSpirit Application",
                    version = config["CodeSpirit:Version"] ?? "1.0.0",
                    framework = RuntimeInformation.FrameworkDescription,
                    os = $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}"
                };
                return Results.Ok(result);
            });
        }

        return app;
    }

    private static void RegisterDatabaseHealthCheck(IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? configuration["Database:Name"]
            ?? configuration["Database:ConnectionString"];

        if (!string.IsNullOrEmpty(connectionString))
        {
            builder.AddCheck("database", () =>
            {
                try
                {
                    return HealthCheckResult.Healthy("Database configured");
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy("Database check failed", ex);
                }
            });
        }
        else
        {
            builder.AddCheck("database", () =>
                HealthCheckResult.Degraded("No database connection string configured"));
        }
    }

    private static void RegisterRedisHealthCheck(IHealthChecksBuilder builder, IConfiguration configuration)
    {
        var redisConnection = configuration["Redis:ConnectionString"]
            ?? configuration["Redis:Configuration"];

        if (!string.IsNullOrEmpty(redisConnection))
        {
            builder.Add(new HealthCheckRegistration(
                "redis",
                sp => new RedisHealthCheck(redisConnection),
                null,
                null,
                TimeSpan.FromSeconds(5)));
        }
        else
        {
            builder.AddCheck("redis", () =>
                HealthCheckResult.Degraded("No Redis connection string configured"));
        }
    }

    private static void RegisterMemoryHealthCheck(IHealthChecksBuilder builder)
    {
        builder.AddCheck("memory", () =>
        {
            var process = Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            var managedMemory = GC.GetTotalMemory(false);

            var threshold = totalMemory > 0
                ? (long)(totalMemory * 0.85)
                : 1024L * 1024 * 1024;

            if (workingSet > threshold)
            {
                return HealthCheckResult.Degraded(
                    "Memory usage high",
                    data: new Dictionary<string, object>
                    {
                        ["workingSet"] = workingSet,
                        ["threshold"] = threshold,
                        ["managedMemory"] = managedMemory
                    });
            }

            return HealthCheckResult.Healthy("Memory usage normal", new Dictionary<string, object>
            {
                ["workingSet"] = workingSet,
                ["managedMemory"] = managedMemory,
                ["totalAvailable"] = totalMemory
            });
        });
    }
}

internal class RedisHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RedisHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await ConnectionMultiplexer.ConnectAsync(
                _connectionString,
                options => options.ConnectTimeout = 3000);

            var db = connection.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis connected");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis unreachable", ex);
        }
    }
}
