using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CodeSpirit.Infrastructure.EntityFramework;

public static class EfCoreStarterExtensions
{
    public static IServiceCollection AddCodeSpiritEntityFrameworkCore(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));
        return services;
    }

    public static IServiceCollection AddCodeSpiritEntityFramework<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : CodeSpiritDbContext
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Sqlite";
        var connectionString = BuildConnectionString(configuration, provider);

        services.AddDbContext<TDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "postgres":
                case "postgresql":
                case "npgsql":
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(TDbContext).Assembly.FullName);
                    });
                    break;

                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(TDbContext).Assembly.FullName);
                    });
                    break;

                case "sqlite":
                default:
                    options.UseSqlite(connectionString, sqliteOptions =>
                    {
                        sqliteOptions.MigrationsAssembly(typeof(TDbContext).Assembly.FullName);
                    });
                    break;
            }
        });

        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));

        return services;
    }

    public static IServiceCollection AddCodeSpiritEntityFramework<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> optionsAction)
        where TDbContext : CodeSpiritDbContext
    {
        services.AddDbContext<TDbContext>(optionsAction);

        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));

        return services;
    }

    public static async Task UseCodeSpiritEntityFramework<TDbContext>(
        this IServiceProvider serviceProvider,
        bool autoMigrate = true)
        where TDbContext : CodeSpiritDbContext
    {
        if (!autoMigrate) return;

        var logger = serviceProvider.GetRequiredService<ILogger<TDbContext>>();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TDbContext>();
            await context.Database.MigrateAsync();
            logger.LogInformation("CodeSpirit EntityFramework auto-migration completed for {DbContextName}", typeof(TDbContext).Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CodeSpirit EntityFramework auto-migration failed for {DbContextName}; continuing startup", typeof(TDbContext).Name);
        }
    }

    private static string BuildConnectionString(IConfiguration configuration, string provider)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        return provider.ToLowerInvariant() switch
        {
            "postgres" or "postgresql" or "npgsql" =>
                BuildNpgsqlConnectionString(configuration),
            "sqlserver" or "mssql" =>
                BuildSqlServerConnectionString(configuration),
            _ =>
                BuildSqliteConnectionString(configuration),
        };
    }

    private static string BuildNpgsqlConnectionString(IConfiguration configuration)
    {
        var host = configuration.GetValue<string>("Database:Host") ?? "localhost";
        var port = configuration.GetValue<int>("Database:Port");
        var database = configuration.GetValue<string>("Database:Name") ?? "codespirit";
        var user = configuration.GetValue<string>("Database:User") ?? "postgres";
        var password = configuration.GetValue<string>("Database:Password") ?? string.Empty;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port > 0 ? port : 5432,
            Database = database,
            Username = user,
            Password = password
        };

        return builder.ConnectionString;
    }

    private static string BuildSqlServerConnectionString(IConfiguration configuration)
    {
        var server = configuration.GetValue<string>("Database:Host") ?? "localhost";
        var database = configuration.GetValue<string>("Database:Name") ?? "codespirit";
        var user = configuration.GetValue<string>("Database:User") ?? string.Empty;
        var password = configuration.GetValue<string>("Database:Password") ?? string.Empty;
        var trustedConnection = configuration.GetValue<bool>("Database:TrustedConnection");

        return trustedConnection
            ? $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;"
            : $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate=True;";
    }

    private static string BuildSqliteConnectionString(IConfiguration configuration)
    {
        var path = configuration.GetValue<string>("Database:Name") ?? "codespirit.db";
        return new SqliteConnectionStringBuilder
        {
            DataSource = path
        }.ConnectionString;
    }
}
