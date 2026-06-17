using System.Reflection;
using CodeSpirit.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Mvvm;

public static class MvvmStarterExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddSingleton<ViewModelExecutor>();

        var vms = assemblies.Length > 0 
            ? Assemblies.Find<ViewModel>(assemblies) 
            : Assemblies.Find<ViewModel>();

        foreach (var vm in vms)
            services.AddTransient(vm);

        return services;
    }
}
