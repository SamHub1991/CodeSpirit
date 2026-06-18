using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;

namespace $safeprojectname$.Features.About;

[PageDirective(Route = "/about", Title = "About")]
[Service]
public class AboutViewModel : ViewModel
{
    [Value("CodeSpirit:Name")]
    [Bind] public string AppName { get; set; } = string.Empty;

    [Value("CodeSpirit:Version")]
    [Bind] public string Version { get; set; } = string.Empty;

    [Bind] public string Description { get; set; } = "A .NET 10 application built with CodeSpirit framework.";

    public override Task LoadAsync()
    {
        return Task.CompletedTask;
    }
}
