using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using CodeSpiritApi.Models;
using CodeSpiritApi.Services;

namespace CodeSpiritApi.ViewModels;

[PageDirective(Route = "/weather", Title = "Weather Forecast")]
[Service]
public class WeatherViewModel : ViewModel
{
    [FromQuery] public string? City { get; set; }
    [Bind] public WeatherForecast[] Forecast { get; set; } = [];

    public override Task LoadAsync()
    {
        var service = Ctx!.Services.GetRequiredService<WeatherService>();
        Forecast = service.GetForecast();
        return Task.CompletedTask;
    }
}
