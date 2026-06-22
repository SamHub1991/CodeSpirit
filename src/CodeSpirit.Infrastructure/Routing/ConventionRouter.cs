using System.Reflection;
using CodeSpirit.Core;
using CodeSpirit.Infrastructure.Mvvm;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Routing;

public static class ConventionRouter
{
    public static IEndpointRouteBuilder MapViewModels(this IEndpointRouteBuilder endpoints, params Assembly[] asms)
    {
        var vms = Assemblies.Find<ViewModel>(asms);

        foreach (var vm in vms)
        {
            var route = ViewModel.GetRoute(vm);
            var captured = vm;

            endpoints.MapGet(route, async ctx =>
                await ctx.RequestServices.GetRequiredService<ViewModelExecutor>().ExecuteAsync(ctx, captured));
            
            endpoints.MapPost(route, async ctx =>
                await ctx.RequestServices.GetRequiredService<ViewModelExecutor>().ExecuteAsync(ctx, captured));
        }
        return endpoints;
    }
}

