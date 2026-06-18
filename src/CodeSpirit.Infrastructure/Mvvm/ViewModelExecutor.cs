using System.Reflection;
using System.Text.Json;
using CodeSpirit.Core;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Infrastructure.Page;
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
    private readonly PageRenderer _pageRenderer;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ViewModelExecutor(ILogger<ViewModelExecutor> logger, PageRenderer pageRenderer)
    {
        _logger = logger;
        _pageRenderer = pageRenderer;
    }

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

        try
        {
            BindQueryParams(vm, ctx.Request.Query);
            BindRouteParams(vm, ctx.GetRouteData().Values);

            var payload = await ReadPayloadAsync(ctx.Request);
            vm.Ctx = new ViewModelContext(ctx, services, ToDictionary(ctx.GetRouteData().Values), payload);

            await vm.InitAsync();
            await vm.LoadAsync();

            BindPostedProperties(vm, payload);
            await ExecuteCommandAsync(vm, GetCommandName(ctx, payload));

            await vm.RenderAsync();

            var state = vm.ToResponse();
            if (HttpMethods.IsGet(ctx.Request.Method))
            {
                await _pageRenderer.RenderAsync(ctx, vmType, state.State);
                return;
            }

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

    private static void BindPostedProperties(ViewModel vm, Dictionary<string, object?> payload)
    {
        if (payload.Count == 0)
            return;

        foreach (var prop in vm.GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<BindAttribute>();
            if (attr is null || !prop.CanWrite || attr.Direction == BindDirection.OneWay)
                continue;

            var name = attr.Name ?? prop.Name;
            if (payload.TryGetValue(name, out var value))
                prop.SetValue(vm, ConvertValue(value, prop.PropertyType));
        }
    }

    private async Task ExecuteCommandAsync(ViewModel vm, string? commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return;

        var command = vm.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(method => new { Method = method, Attribute = method.GetCustomAttribute<CommandAttribute>() })
            .FirstOrDefault(x => x.Attribute is not null &&
                string.Equals(x.Attribute.Name ?? x.Method.Name, commandName, StringComparison.OrdinalIgnoreCase));

        if (command is null)
            throw new InvalidOperationException($"Command '{commandName}' was not found on ViewModel '{vm.GetType().Name}'.");

        if (command.Method.GetParameters().Length > 0)
            throw new InvalidOperationException($"Command '{commandName}' must not declare parameters.");

        var result = command.Method.Invoke(vm, null);
        if (result is Task task)
            await task;
    }

    private static string? GetCommandName(HttpContext ctx, Dictionary<string, object?> payload)
    {
        if (payload.TryGetValue("__command", out var command) || payload.TryGetValue("command", out command))
            return command?.ToString();

        if (ctx.Request.Query.TryGetValue("__command", out var queryCommand) ||
            ctx.Request.Query.TryGetValue("command", out queryCommand))
            return queryCommand.FirstOrDefault();

        return null;
    }

    private static async Task<Dictionary<string, object?>> ReadPayloadAsync(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method) && !HttpMethods.IsPatch(request.Method))
            return [];

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            return form.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value.FirstOrDefault());
        }

        if (request.ContentLength == 0)
            return [];

        var payload = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(request.Body, JsonOptions);
        if (payload is null)
            return [];

        return payload.ToDictionary(kvp => kvp.Key, kvp => ConvertJsonElement(kvp.Value));
    }

    private static Dictionary<string, object?> ToDictionary(RouteValueDictionary route)
    {
        return route.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    private static object? ConvertValue(object? value, Type target)
    {
        if (value is null)
            return null;

        var underlying = Nullable.GetUnderlyingType(target) ?? target;
        if (underlying.IsInstanceOfType(value))
            return value;

        var stringValue = value.ToString();
        return underlying switch
        {
            Type t when t == typeof(string) => stringValue,
            Type t when t == typeof(int) && int.TryParse(stringValue, out var i) => i,
            Type t when t == typeof(long) && long.TryParse(stringValue, out var l) => l,
            Type t when t == typeof(decimal) && decimal.TryParse(stringValue, out var d) => d,
            Type t when t == typeof(double) && double.TryParse(stringValue, out var dbl) => dbl,
            Type t when t == typeof(Guid) && Guid.TryParse(stringValue, out var g) => g,
            Type t when t == typeof(DateTime) && DateTime.TryParse(stringValue, out var dt) => dt,
            Type t when t == typeof(bool) && bool.TryParse(stringValue, out var b) => b,
            Type t when t.IsEnum => Enum.Parse(t, stringValue!, ignoreCase: true),
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
