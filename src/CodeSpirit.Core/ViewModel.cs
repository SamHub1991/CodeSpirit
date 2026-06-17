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
}

/// <summary>
/// Context passed to ViewModel with request information.
/// </summary>
public record ViewModelContext(
    HttpContext HttpContext,
    IServiceProvider Services,
    Dictionary<string, object?> Route,
    Dictionary<string, object?> Payload);
