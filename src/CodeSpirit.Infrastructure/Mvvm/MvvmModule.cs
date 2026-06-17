using CodeSpirit.Core;
using CodeSpirit.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Mvvm;

/// <summary>
/// Registers all ViewModels in CodeSpirit assemblies.
/// </summary>
[Require(typeof(ViewModel))]
public class MvvmModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddViewModels(Assemblies.CodeSpirit);
    }
}
