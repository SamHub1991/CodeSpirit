using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using $safeprojectname$.Models;
using $safeprojectname$.Services;

namespace $safeprojectname$.ViewModels;

[PageDirective(Route = "/weather", Title = "Weather Forecast", Layout = "~/Pages/Site.master")]
[Service]
public class WeatherViewModel : ViewModel
{
    [FromQuery] public string? City { get; set; }
    [Bind] public WeatherForecast[] Forecast { get; set; } = [];
    [Bind] public bool HasForecast => Forecast.Length > 0;

    public override Task LoadAsync()
    {
        var service = Ctx!.Services.GetRequiredService<WeatherService>();
        Forecast = service.GetForecast();
        return Task.CompletedTask;
    }
}