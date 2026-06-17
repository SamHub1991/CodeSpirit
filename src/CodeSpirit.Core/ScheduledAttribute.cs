namespace CodeSpirit.Core;

/// <summary>
/// Marks a method as a scheduled task.
/// The method will be executed automatically on the specified cron schedule.
///
/// Usage:
///   [Scheduled("0 */5 * * * ?")]  // every 5 minutes
///   [Scheduled("0 0 8 * * ?", Name = "MorningReport")]
///   public async Task RunDailyReport() { ... }
///
/// Convention: method name becomes the job name if Name is not specified.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ScheduledAttribute : Attribute
{
    public string Cron { get; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public ScheduledAttribute(string cron) => Cron = cron;
}

/// <summary>
/// Marks a method as a fixed-rate scheduled task (interval in seconds).
/// Simpler alternative to cron for periodic tasks.
///
/// Usage:
///   [Every(300)]  // every 300 seconds = 5 minutes
///   public async Task HealthCheck() { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class EveryAttribute : Attribute
{
    public int Seconds { get; }
    public string? Name { get; set; }

    public EveryAttribute(int seconds) => Seconds = seconds;
}

/// <summary>
/// Marks a method as a delayed startup task.
/// Runs once after the application starts, with optional delay.
///
/// Usage:
///   [OnStartup]           // run immediately on startup
///   [OnStartup(5000)]     // run 5 seconds after startup
///   public async Task WarmCache() { ... }
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class OnStartupAttribute : Attribute
{
    public int DelayMs { get; }

    public OnStartupAttribute() => DelayMs = 0;
    public OnStartupAttribute(int delayMs) => DelayMs = delayMs;
}
