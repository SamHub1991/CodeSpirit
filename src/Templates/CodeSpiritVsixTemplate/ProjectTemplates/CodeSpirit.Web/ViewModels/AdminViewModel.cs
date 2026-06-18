using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using CodeSpirit.Web.Models;
using CodeSpirit.Web.Services;

namespace CodeSpirit.Web.ViewModels;

[PageDirective(Route = "/admin", Title = "Library Admin", Layout = "~/Pages/Site.master")]
[Service]
public class AdminViewModel : ViewModel
{
    [Bind] public List<BookItem> Books { get; set; } = [];
    [Bind] public List<ReaderItem> Readers { get; set; } = [];
    [Bind] public List<LoanRecord> Loans { get; set; } = [];
    [Bind] public List<ReservationItem> Reservations { get; set; } = [];
    [Bind] public List<LibraryMetric> Metrics { get; set; } = [];
    [Bind] public List<CategoryStat> CategoryStats { get; set; } = [];
    [Bind] public List<LibraryActivity> Activities { get; set; } = [];
    [Bind] public List<AdminNotice> Notices { get; set; } = [];

    [Bind(BindDirection.TwoWay)] public string Query { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string FilterStatus { get; set; } = "All";
    [Bind(BindDirection.TwoWay)] public string FilterCategory { get; set; } = "All";

    [Bind(BindDirection.TwoWay)] public int BookId { get; set; }
    [Bind(BindDirection.TwoWay)] public string BookIsbn { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string BookTitle { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string BookAuthor { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string BookCategory { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string BookLocation { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public int BookPublishedYear { get; set; }
    [Bind(BindDirection.TwoWay)] public int BookCopyCount { get; set; } = 1;

    [Bind(BindDirection.TwoWay)] public int ReaderId { get; set; }
    [Bind(BindDirection.TwoWay)] public string ReaderName { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string ReaderEmail { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string ReaderPhone { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string ReaderLevel { get; set; } = "Standard";

    [Bind(BindDirection.TwoWay)] public int LoanId { get; set; }
    [Bind(BindDirection.TwoWay)] public string LoanReaderId { get; set; } = string.Empty;

    [Bind(BindDirection.TwoWay)] public int ReservationId { get; set; }
    [Bind(BindDirection.TwoWay)] public string ReservationReaderId { get; set; } = string.Empty;

    [Bind(BindDirection.TwoWay)] public decimal FineAmount { get; set; }

    public override Task LoadAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    [Command]
    public void Search()
    {
        Refresh();
    }

    [Command]
    public void AddBook()
    {
        Notices = [Service.AddBook(BookIsbn, BookTitle, BookAuthor, BookCategory, BookLocation, BookPublishedYear, BookCopyCount)];
        ClearBookForm();
        Refresh();
    }

    [Command]
    public void UpdateBook()
    {
        Notices = [Service.UpdateBook(BookId, BookIsbn, BookTitle, BookAuthor, BookCategory, BookLocation, BookPublishedYear, BookCopyCount)];
        Refresh();
    }

    [Command]
    public void ArchiveBook()
    {
        Notices = [Service.ArchiveBook(BookId)];
        Refresh();
    }

    [Command]
    public void RestoreBook()
    {
        Notices = [Service.RestoreBook(BookId)];
        Refresh();
    }

    [Command]
    public void RegisterReader()
    {
        Notices = [Service.RegisterReader(ReaderName, ReaderEmail, ReaderPhone, ReaderLevel)];
        ClearReaderForm();
        Refresh();
    }

    [Command]
    public void UpdateReader()
    {
        Notices = [Service.UpdateReader(ReaderId, ReaderName, ReaderEmail, ReaderPhone, ReaderLevel)];
        Refresh();
    }

    [Command]
    public void SuspendReader()
    {
        Notices = [Service.SuspendReader(ReaderId)];
        Refresh();
    }

    [Command]
    public void ActivateReader()
    {
        Notices = [Service.ActivateReader(ReaderId)];
        Refresh();
    }

    [Command]
    public void BorrowBook()
    {
        Notices = [Service.BorrowBook(BookId, ParseId(LoanReaderId))];
        Refresh();
    }

    [Command]
    public void ReturnBook()
    {
        Notices = [Service.ReturnBook(LoanId)];
        Refresh();
    }

    [Command]
    public void RenewLoan()
    {
        Notices = [Service.RenewLoan(LoanId)];
        Refresh();
    }

    [Command]
    public void ReserveBook()
    {
        Notices = [Service.ReserveBook(BookId, ParseId(ReservationReaderId))];
        Refresh();
    }

    [Command]
    public void CancelReservation()
    {
        Notices = [Service.CancelReservation(ReservationId)];
        Refresh();
    }

    [Command]
    public void CollectFine()
    {
        Notices = [Service.CollectFine(ReaderId, FineAmount)];
        Refresh();
    }

    private LibraryService Service => Ctx.Services.GetRequiredService<LibraryService>();

    private void Refresh()
    {
        var snapshot = Service.GetSnapshot(Query, FilterStatus, FilterCategory);
        Books = snapshot.Books;
        Readers = snapshot.Readers;
        Loans = snapshot.Loans;
        Reservations = snapshot.Reservations;
        Metrics = snapshot.Metrics;
        CategoryStats = snapshot.CategoryStats;
        Activities = snapshot.Activities;
    }

    private void ClearBookForm()
    {
        BookId = 0;
        BookIsbn = string.Empty;
        BookTitle = string.Empty;
        BookAuthor = string.Empty;
        BookCategory = string.Empty;
        BookLocation = string.Empty;
        BookPublishedYear = 0;
        BookCopyCount = 1;
    }

    private void ClearReaderForm()
    {
        ReaderId = 0;
        ReaderName = string.Empty;
        ReaderEmail = string.Empty;
        ReaderPhone = string.Empty;
        ReaderLevel = "Standard";
    }

    private static int ParseId(string value) => int.TryParse(value, out var result) ? result : 0;
}
