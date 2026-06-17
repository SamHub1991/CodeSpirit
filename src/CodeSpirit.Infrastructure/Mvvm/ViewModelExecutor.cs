using System.Reflection;
using System.Text.Json;
using CodeSpirit.Core;
using CodeSpirit.Core.Mvvm;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Infrastructure.Mvvm;

/// <summary>
/// Executes ViewModels and handles the request lifecycle.
/// </summary>
public class ViewModelExecutor
{
    private readonly ILogger<ViewModelExecutor> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ViewModelExecutor(ILogger<ViewModelExecutor> logger) => _logger = logger;

    public async Task ExecuteAsync(HttpContext ctx, Type vmType)
    {
        var services = ctx.RequestServices;
        ViewModel vm;

        try { vm = (ViewModel)services.GetRequiredService(vmType); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot resolve {Vm}", vmType.Name);
            await WriteError(ctx, 500, $"ViewModel '{vmType.Name}' not found");
            return;
        }

        // Bind query parameters to [FromQuery] properties
        BindQueryParams(vm, ctx.Request.Query);
        
        // Bind route parameters to [FromRoute] properties  
        BindRouteParams(vm, ctx.GetRouteData().Values);

        vm.Ctx = new ViewModelContext(ctx, services, new(), new());

        try
        {
            await vm.InitAsync();
            await vm.LoadAsync();
            await vm.RenderAsync();
            
            var state = vm.ToState();
            await WriteJson(ctx, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VM {Name} failed", vmType.Name);
            await WriteError(ctx, 500, ex.Message);
        }
    }

    private void BindQueryParams(ViewModel vm, IQueryCollection query)
    {
        foreach (var prop in vm.GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<FromQueryAttribute>();
            if (attr is null) continue;

            var name = attr.Name ?? prop.Name;
            if (query.TryGetValue(name, out var value) && value.Count > 0)
            {
                var strValue = value[0];
                if (strValue is not null)
                    prop.SetValue(vm, ConvertValue(strValue, prop.PropertyType));
            }
        }
    }

    private void BindRouteParams(ViewModel vm, RouteValueDictionary route)
    {
        foreach (var prop in vm.GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<FromRouteAttribute>();
            if (attr is null) continue;

            var name = attr.Name ?? prop.Name;
            if (route.TryGetValue(name, out var value) && value is not null)
            {
                var strValue = value.ToString();
                if (strValue is not null)
                    prop.SetValue(vm, ConvertValue(strValue, prop.PropertyType));
            }
        }
    }

    private static object? ConvertValue(string? value, Type target)
    {
        var underlying = Nullable.GetUnderlyingType(target) ?? target;
        return underlying switch
        {
            Type t when t == typeof(string) => value,
            Type t when t == typeof(int) && int.TryParse(value, out var i) => i,
            Type t when t == typeof(long) && long.TryParse(value, out var l) => l,
            Type t when t == typeof(Guid) && Guid.TryParse(value, out var g) => g,
            Type t when t == typeof(bool) && bool.TryParse(value, out var b) => b,
            _ => Convert.ChangeType(value, underlying)
        };
    }

    private static async Task WriteJson(HttpContext ctx, object data)
    {
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, data, JsonOptions);
    }

    private static async Task WriteError(HttpContext ctx, int code, string msg)
    {
        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, new { error = msg });
    }
}
