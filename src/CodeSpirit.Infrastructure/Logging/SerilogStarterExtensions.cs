using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace CodeSpirit.Infrastructure.Logging;

public static class SerilogStarterExtensions
{
    public static IHostBuilder UseCodeSpiritSerilog(
        this IHostBuilder hostBuilder, IConfiguration configuration)
    {
        return hostBuilder.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console();
        });
    }

    public static LoggerConfiguration EnrichCodeSpiritSerilog(
        this LoggerConfiguration loggerConfig, IConfiguration configuration)
    {
        return loggerConfig
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console();
    }
}
