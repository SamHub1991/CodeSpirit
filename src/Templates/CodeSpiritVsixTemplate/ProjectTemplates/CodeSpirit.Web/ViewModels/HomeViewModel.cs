using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using $safeprojectname$.Models;
using $safeprojectname$.Services;

namespace $safeprojectname$.ViewModels;

[PageDirective(Route = "/", Title = "Library Command Center", Layout = "~/Pages/Site.master")]
[Service]
public class HomeViewModel : ViewModel
{
    [Bind] public string WelcomeMessage { get; set; } = "Library Command Center";
    [Bind] public string Subtitle { get; set; } = "Real-time collection, circulation, and reading insights";
    [Bind] public List<LibraryMetric> Metrics { get; set; } = [];
    [Bind] public List<CategoryStat> CategoryStats { get; set; } = [];
    [Bind] public List<BookItem> PopularBooks { get; set; } = [];
    [Bind] public List<LibraryActivity> Activities { get; set; } = [];
    [Bind] public List<DashboardCard> Cards { get; set; } = [];

    public override Task LoadAsync()
    {
        var snapshot = Ctx.Services.GetRequiredService<LibraryService>().GetSnapshot();
        Metrics = snapshot.Metrics;
        CategoryStats = snapshot.CategoryStats;
        PopularBooks = snapshot.PopularBooks;
        Activities = snapshot.Activities;
        Cards =
        [
            new() { Title = "Backend Admin", Description = "Manage books, borrowing, and returns", Url = "/admin" },
            new() { Title = "Health", Description = "Application health status", Url = "/actuator/health" },
            new() { Title = "About", Description = "Framework and app information", Url = "/about" }
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
