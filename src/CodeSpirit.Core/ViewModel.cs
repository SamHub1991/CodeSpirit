using System.Reflection;
using Microsoft.AspNetCore.Http;
using CodeSpirit.Core.Mvvm;

namespace CodeSpirit.Core;

/// <summary>
/// Base class for MVVM ViewModels.
/// Simple lifecycle: Init -> Load -> Render
/// </summary>
public abstract class ViewModel
{
    /// <summary>
    /// HTTP context for the current request.
    /// </summary>
    public HttpContext Http => Ctx.HttpContext;

    /// <summary>
    /// Services available for dependency injection.
    /// </summary>
    public IServiceProvider Services => Ctx.Services;

    /// <summary>
    /// Route parameters from the URL.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Route => Ctx.Route;

    /// <summary>
    /// POST body payload (JSON deserialized).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Payload => Ctx.Payload;

    // Internal context set by the executor
    public ViewModelContext Ctx { get; set; } = null!;

    /// <summary>
    /// Called once when the page first loads.
    /// Override to perform initialization.
    /// </summary>
    public virtual Task InitAsync() => Task.CompletedTask;

    /// <summary>
    /// Called on every request (GET/POST).
    /// Override to load data.
    /// </summary>
    public virtual Task LoadAsync() => Task.CompletedTask;

    /// <summary>
    /// Called after LoadAsync, before rendering.
    /// Override for final processing.
    /// </summary>
    public virtual Task RenderAsync() => Task.CompletedTask;

    /// <summary>
    /// Returns all [Bind] properties as a dictionary for JSON serialization.
    /// </summary>
    public Dictionary<string, object?> ToState()
    {
        var state = new Dictionary<string, object?>();
        foreach (var prop in GetType().GetProperties())
        {
            if (prop.GetCustomAttribute<BindAttribute>() is not null)
                state[prop.Name] = prop.GetValue(this);
        }
        return state;
    }

    /// <summary>
    /// Returns ViewModel state plus binding and command metadata for page runtimes.
    /// </summary>
    public ViewModelResponse ToResponse()
    {
        var bindings = new Dictionary<string, BindingDescriptor>();

        foreach (var prop in GetType().GetProperties())
        {
            var attr = prop.GetCustomAttribute<BindAttribute>();
            if (attr is null)
                continue;

            var name = attr.Name ?? prop.Name;
            bindings[name] = new BindingDescriptor(name, prop.Name, attr.Direction.ToString());
        }

        var commands = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(method => new { Method = method, Attribute = method.GetCustomAttribute<CommandAttribute>() })
            .Where(x => x.Attribute is not null)
            .Select(x => x.Attribute!.Name ?? x.Method.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ViewModelResponse(ToState(), bindings, commands);
    }
}

/// <summary>
/// Context passed to ViewModel with request information.
/// </summary>
public record ViewModelContext(
    HttpContext HttpContext,
    IServiceProvider Services,
    Dictionary<string, object?> Route,
    Dictionary<string, object?> Payload);

public record BindingDescriptor(string Name, string Property, string Direction);

public record ViewModelResponse(
    Dictionary<string, object?> State,
    Dictionary<string, BindingDescriptor> Bindings,
    string[] Commands)
{
    public Dictionary<string, string> Regions { get; init; } = [];
}
