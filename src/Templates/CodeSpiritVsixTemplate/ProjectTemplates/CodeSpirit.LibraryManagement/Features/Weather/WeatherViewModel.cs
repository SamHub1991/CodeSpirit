using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using $safeprojectname$.Features.Weather.Models;
using $safeprojectname$.Features.Weather.Services;

namespace $safeprojectname$.Features.Weather;

[PageDirective(Route = "/weather", Title = "Weather Forecast")]
[Service]
public class WeatherViewModel : ViewModel
{
    [FromQuery]
    [Bind(BindDirection.TwoWay)]
    public string? City { get; set; }

    [Bind] public WeatherForecast[] Forecast { get; set; } = [];
    [Bind] public bool HasForecast => Forecast.Length > 0;

    public override Task LoadAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    [Command]
    public void Refresh()
    {
        var service = Ctx!.Services.GetRequiredService<WeatherService>();
        Forecast = service.GetForecast(City);
    }
}
