using System.Reflection;
using CodeSpirit.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CodeSpirit.Infrastructure.Scheduling;

/// <summary>
/// Scans methods annotated with [Scheduled], [Every], or [OnStartup]
/// and registers them with Quartz or runs them on startup.
///
/// Convention: method name = job name if not specified.
/// </summary>
public static class SchedulingExtensions
{
    public static IServiceCollection AddScheduling(this IServiceCollection services, IConfiguration config, params Assembly[] asms)
    {
        var targets = Assemblies.Find<object>(asms.Length > 0 ? asms : null);
        var scheduledMethods = new List<ScheduledMethod>();
        var intervalMethods = new List<IntervalMethod>();

        foreach (var type in targets)
        {
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var sched = method.GetCustomAttribute<ScheduledAttribute>();
                if (sched is not null)
                {
                    services.AddTransient(type);
                    scheduledMethods.Add(new(type, method, sched.Cron, sched.Name ?? method.Name));
                }

                var every = method.GetCustomAttribute<EveryAttribute>();
                if (every is not null)
                {
                    services.AddTransient(type);
                    intervalMethods.Add(new(type, method, TimeSpan.FromSeconds(every.Seconds), every.Name ?? method.Name));
                }
            }
        }

        if (scheduledMethods.Count == 0 && intervalMethods.Count == 0)
            return services;

        services.AddQuartz(q =>
        {
            // Cron-based scheduled jobs
            foreach (var sm in scheduledMethods)
            {
                var key = new JobKey(sm.Name);
                q.AddJob<MethodJobWrapper>(opts =>
                {
                    opts.WithIdentity(key);
                    opts.UsingJobData("TargetType", sm.TargetType.AssemblyQualifiedName!);
                    opts.UsingJobData("MethodName", sm.Method.Name);
                });

                q.AddTrigger(opts =>
                {
                    opts.ForJob(key);
                    opts.WithCronSchedule(sm.Cron, x => x.WithMisfireHandlingInstructionDoNothing());
                    opts.WithIdentity($"{sm.Name}-trigger");
                });
            }

            // Interval-based jobs (using SimpleSchedule)
            foreach (var im in intervalMethods)
            {
                var key = new JobKey(im.Name);
                q.AddJob<MethodJobWrapper>(opts =>
                {
                    opts.WithIdentity(key);
                    opts.UsingJobData("TargetType", im.TargetType.AssemblyQualifiedName!);
                    opts.UsingJobData("MethodName", im.Method.Name);
                });

                q.AddTrigger(opts =>
                {
                    opts.ForJob(key);
                    opts.WithSimpleSchedule(x => x
                        .WithInterval(im.Interval)
                        .RepeatForever()
                        .WithMisfireHandlingInstructionFireNow());
                    opts.WithIdentity($"{im.Name}-trigger");
                });
            }
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Handle [OnStartup] methods
        var startupMethods = targets
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(m => (Type: t, Method: m, Attr: m.GetCustomAttribute<OnStartupAttribute>()))
                .Where(x => x.Attr is not null)
                .Select(x => (x.Type, x.Method, Attr: x.Attr!)));

        foreach (var (type, method, attr) in startupMethods)
            services.AddTransient(type);

        if (startupMethods.Any())
            services.AddSingleton<IHostedService>(sp => new StartupRunner(sp, startupMethods.ToList()));

        return services;
    }

    private record ScheduledMethod(Type TargetType, MethodInfo Method, string Cron, string Name);
    private record IntervalMethod(Type TargetType, MethodInfo Method, TimeSpan Interval, string Name);
}

/// <summary>
/// Quartz job that invokes a method annotated with [Scheduled].
/// </summary>
public class MethodJobWrapper : IJob
{
    private readonly IServiceProvider _sp;

    public MethodJobWrapper(IServiceProvider sp) => _sp = sp;

    public async Task Execute(IJobExecutionContext ctx)
    {
        var typeName = ctx.JobDetail.JobDataMap.GetString("TargetType");
        var methodName = ctx.JobDetail.JobDataMap.GetString("MethodName");
        if (typeName is null || methodName is null) return;

        var type = Type.GetType(typeName);
        if (type is null) return;

        await using var scope = _sp.CreateAsyncScope();
        var instance = scope.ServiceProvider.GetRequiredService(type);
        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method is null) return;

        var result = method.Invoke(instance, null);
        if (result is Task task) await task;
    }
}

/// <summary>
/// Runs [OnStartup] methods after the application starts.
/// </summary>
public class StartupRunner : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly List<(Type Type, MethodInfo Method, OnStartupAttribute Attr)> _methods;

    public StartupRunner(IServiceProvider sp, List<(Type, MethodInfo, OnStartupAttribute)> methods)
    {
        _sp = sp;
        _methods = methods;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        foreach (var (type, method, attr) in _methods)
        {
            if (attr.DelayMs > 0)
                await Task.Delay(attr.DelayMs, ct);

            await using var scope = _sp.CreateAsyncScope();
            var instance = scope.ServiceProvider.GetRequiredService(type);
            var result = method.Invoke(instance, null);
            if (result is Task task) await task;
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
