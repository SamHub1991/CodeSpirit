using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using $safeprojectname$.Models;
using $safeprojectname$.Services;

namespace $safeprojectname$.ViewModels;

[PageDirective(Route = "/admin", Title = "Library Admin", Layout = "~/Pages/Site.master")]
[Service]
public class AdminViewModel : ViewModel
{
    [Bind] public List<BookItem> Books { get; set; } = [];
    [Bind] public List<LibraryMetric> Metrics { get; set; } = [];
    [Bind] public List<LibraryActivity> Activities { get; set; } = [];

    [Bind(BindDirection.TwoWay)] public string NewTitle { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string NewAuthor { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string NewCategory { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string NewLocation { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public int BookId { get; set; }
    [Bind(BindDirection.TwoWay)] public string Borrower { get; set; } = string.Empty;

    public override Task LoadAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    [Command]
    public void AddBook()
    {
        Service.AddBook(NewTitle, NewAuthor, NewCategory, NewLocation);
        NewTitle = string.Empty;
        NewAuthor = string.Empty;
        NewCategory = string.Empty;
        NewLocation = string.Empty;
        Refresh();
    }

    [Command]
    public void BorrowBook()
    {
        Service.BorrowBook(BookId, Borrower);
        Borrower = string.Empty;
        Refresh();
    }

    [Command]
    public void ReturnBook()
    {
        Service.ReturnBook(BookId);
        Refresh();
    }

    private LibraryService Service => Ctx.Services.GetRequiredService<LibraryService>();

    private void Refresh()
    {
        var snapshot = Service.GetSnapshot();
        Books = snapshot.Books;
        Metrics = snapshot.Metrics;
        Activities = snapshot.Activities;
    }
}
