using System.Reflection;
using System.Text;
using CodeSpirit.Core;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Infrastructure.Mvvm;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Routing;

/// <summary>
/// Convention-based routing for ViewModels.
/// Maps /Customer -> CustomerViewModel, /Order/List -> OrderListViewModel
/// </summary>
public static class ConventionRouter
{
    public static IEndpointRouteBuilder MapViewModels(this IEndpointRouteBuilder endpoints, params Assembly[] asms)
    {
        var vms = Assemblies.Find<ViewModel>(asms);

        foreach (var vm in vms)
        {
            var route = GetRoute(vm);
            var captured = vm;

            endpoints.MapGet(route, async ctx =>
                await ctx.RequestServices.GetRequiredService<ViewModelExecutor>().ExecuteAsync(ctx, captured));
            
            endpoints.MapPost(route, async ctx =>
                await ctx.RequestServices.GetRequiredService<ViewModelExecutor>().ExecuteAsync(ctx, captured));
        }
        return endpoints;
    }

    private static string GetRoute(Type vm)
    {
        var attr = vm.GetCustomAttribute<CodeSpirit.Core.Page.PageDirectiveAttribute>();
        if (attr?.Route is { } explicitRoute) return explicitRoute;

        var name = vm.Name;
        if (name.EndsWith("ViewModel")) name = name[..^"ViewModel".Length];
        return "/" + string.Concat(name.Select((c, i) => 
            i > 0 && char.IsUpper(c) ? "-" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}

