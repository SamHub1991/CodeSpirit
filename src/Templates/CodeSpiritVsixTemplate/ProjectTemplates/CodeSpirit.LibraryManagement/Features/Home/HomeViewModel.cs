using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using $safeprojectname$.Features.Library.Models;
using $safeprojectname$.Features.Library.Services;

namespace $safeprojectname$.Features.Home;

[PageDirective(Route = "/", Title = "Library Command Center")]
[Service]
public class HomeViewModel : ViewModel
{
    [Bind] public string WelcomeMessage { get; set; } = "Library Command Center";
    [Bind] public string Subtitle { get; set; } = "Real-time collection, circulation, and reading insights";
    [Bind] public List<LibraryMetric> Metrics { get; set; } = [];
    [Bind] public List<CategoryStat> CategoryStats { get; set; } = [];
    [Bind] public List<BookItem> PopularBooks { get; set; } = [];
    [Bind] public List<LoanRecord> ActiveLoans { get; set; } = [];
    [Bind] public List<ReservationItem> Reservations { get; set; } = [];
    [Bind] public List<LibraryActivity> Activities { get; set; } = [];
    [Bind] public List<DashboardCard> Cards { get; set; } = [];

    public override Task LoadAsync()
    {
        var snapshot = Ctx.Services.GetRequiredService<LibraryService>().GetSnapshot();
        Metrics = snapshot.Metrics;
        CategoryStats = snapshot.CategoryStats;
        PopularBooks = snapshot.PopularBooks;
        ActiveLoans = snapshot.Loans.Where(loan => loan.Status is "Active" or "Overdue").Take(5).ToList();
        Reservations = snapshot.Reservations.Where(item => item.Status == "Waiting").Take(5).ToList();
        Activities = snapshot.Activities;
        Cards =
        [
            new() { Title = "Backend Admin", Description = "Manage books, readers, loans, reservations, and fines", Url = "/admin" },
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
