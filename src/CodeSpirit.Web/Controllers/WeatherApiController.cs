using CodeSpirit.Web.Models;
using CodeSpirit.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeSpirit.Web.Controllers;

// REST API controller - returns JSON
// Page routing is handled by ViewModels with [PageDirective]
[ApiController]
[Route("api/[controller]")]
public class WeatherApiController : ControllerBase
{
    private readonly WeatherService _weatherService;

    public WeatherApiController(WeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet]
    public ActionResult<WeatherForecast[]> GetForecast()
    {
        return Ok(_weatherService.GetForecast());
    }
}