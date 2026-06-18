using CodeSpirit.Core.Attributes;
using CodeSpirit.Web.Models;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Web.Services;

[Service]
public class WeatherService
{
    [Autowired]
    private ILogger<WeatherService> _logger = null!;

    [Value("CodeSpirit:Name")]
    public string AppName { get; set; } = string.Empty;

    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public WeatherForecast[] GetForecast()
    {
        _logger.LogInformation("[{App}] Generating weather forecast", AppName);
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
}
