using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;

namespace CodeSpirit.Web.ViewModels;

[PageDirective(Route = "/", Title = "Home", Layout = "~/Pages/Site.master")]
[Service]
public class HomeViewModel : ViewModel
{
    [Bind] public string WelcomeMessage { get; set; } = "Welcome to CodeSpirit.Web";
    [Bind] public string Subtitle { get; set; } = "Built with CodeSpirit Framework";
    [Bind] public List<DashboardCard> Cards { get; set; } = [];

    public override Task LoadAsync()
    {
        Cards =
        [
            new() { Title = "Weather", Description = "Check the weather forecast", Url = "/weather" },
            new() { Title = "About", Description = "Learn more about this application", Url = "/about" },
            new() { Title = "Health", Description = "Application health status", Url = "/actuator/health" },
        ];
        return Task.CompletedTask;
    }

    public class DashboardCard
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
