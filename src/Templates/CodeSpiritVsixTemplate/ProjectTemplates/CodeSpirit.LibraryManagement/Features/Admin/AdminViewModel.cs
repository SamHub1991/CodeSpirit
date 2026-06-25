using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;
using $safeprojectname$.Features.Library.Models;
using $safeprojectname$.Features.Library.Services;

namespace $safeprojectname$.Features.Admin;

[PageDirective(Route = "/admin", Title = "Library Admin")]
[Service]
public class AdminViewModel : CodeSpirit.Core.ViewModel
{
    [Bind] public List<BookItem> Books { get; set; } = [];
    [Bind] public List<ReaderItem> Readers { get; set; } = [];
    [Bind] public List<LoanRecord> Loans { get; set; } = [];
    [Bind] public List<ReservationItem> Reservations { get; set; } = [];
    [Bind] public List<InventoryEvent> InventoryEvents { get; set; } = [];
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

    [Bind(BindDirection.TwoWay)] public int InventoryBookId { get; set; }
    [Bind(BindDirection.TwoWay)] public int InventoryQuantity { get; set; } = 1;
    [Bind(BindDirection.TwoWay)] public string InventoryLocation { get; set; } = string.Empty;
    [Bind(BindDirection.TwoWay)] public string InventoryReason { get; set; } = string.Empty;

    [Bind(BindDirection.TwoWay)] public string ImportExportCsv { get; set; } = string.Empty;

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
        if (!ValidateAddBook())
            return;

        Notices = [Service.AddBook(BookIsbn, BookTitle, BookAuthor, BookCategory, BookLocation, BookPublishedYear, BookCopyCount)];
        ClearBookForm();
        Refresh();
    }

    [Command]
    public void UpdateBook()
    {
        if (!ValidateUpdateBook())
            return;

        Notices = [Service.UpdateBook(BookId, BookIsbn, BookTitle, BookAuthor, BookCategory, BookLocation, BookPublishedYear, BookCopyCount)];
        Refresh();
    }

    [Command]
    public void ArchiveBook()
    {
        if (!EnsurePositive(BookId, nameof(BookId), "Book Id is required."))
            return;

        Notices = [Service.ArchiveBook(BookId)];
        Refresh();
    }

    [Command]
    public void RestoreBook()
    {
        if (!EnsurePositive(BookId, nameof(BookId), "Book Id is required."))
            return;

        Notices = [Service.RestoreBook(BookId)];
        Refresh();
    }

    [Command]
    public void RegisterReader()
    {
        if (!ValidateReaderProfile(includeReaderId: false))
            return;

        Notices = [Service.RegisterReader(ReaderName, ReaderEmail, ReaderPhone, ReaderLevel)];
        ClearReaderForm();
        Refresh();
    }

    [Command]
    public void UpdateReader()
    {
        if (!ValidateReaderProfile(includeReaderId: true))
            return;

        Notices = [Service.UpdateReader(ReaderId, ReaderName, ReaderEmail, ReaderPhone, ReaderLevel)];
        Refresh();
    }

    [Command]
    public void SuspendReader()
    {
        if (!EnsurePositive(ReaderId, nameof(ReaderId), "Reader Id is required."))
            return;

        Notices = [Service.SuspendReader(ReaderId)];
        Refresh();
    }

    [Command]
    public void ActivateReader()
    {
        if (!EnsurePositive(ReaderId, nameof(ReaderId), "Reader Id is required."))
            return;

        Notices = [Service.ActivateReader(ReaderId)];
        Refresh();
    }

    [Command]
    public void BorrowBook()
    {
        if (!ValidateLoanPair())
            return;

        Notices = [Service.BorrowBook(BookId, ParseId(LoanReaderId))];
        Refresh();
    }

    [Command]
    public void ReturnBook()
    {
        if (!EnsurePositive(LoanId, nameof(LoanId), "Loan Id is required."))
            return;

        Notices = [Service.ReturnBook(LoanId)];
        Refresh();
    }

    [Command]
    public void RenewLoan()
    {
        if (!EnsurePositive(LoanId, nameof(LoanId), "Loan Id is required."))
            return;

        Notices = [Service.RenewLoan(LoanId)];
        Refresh();
    }

    [Command]
    public void ReserveBook()
    {
        if (!ValidateReservationPair())
            return;

        Notices = [Service.ReserveBook(BookId, ParseId(ReservationReaderId))];
        Refresh();
    }

    [Command]
    public void CancelReservation()
    {
        if (!EnsurePositive(ReservationId, nameof(ReservationId), "Reservation Id is required."))
            return;

        Notices = [Service.CancelReservation(ReservationId)];
        Refresh();
    }

    [Command]
    public void CollectFine()
    {
        var valid = true;
        valid &= EnsurePositive(ReaderId, nameof(ReaderId), "Reader Id is required.");
        valid &= EnsurePositive(FineAmount, nameof(FineAmount), "Fine amount must be greater than zero.");
        if (!valid)
            return;

        Notices = [Service.CollectFine(ReaderId, FineAmount)];
        Refresh();
    }

    [Command]
    public void ReceiveCopies()
    {
        if (!ValidateInventoryAction(requireLocation: false))
            return;

        Notices = [Service.ReceiveCopies(InventoryBookId, InventoryQuantity, InventoryReason)];
        ClearInventoryForm();
        Refresh();
    }

    [Command]
    public void WriteOffCopies()
    {
        if (!ValidateInventoryAction(requireLocation: false))
            return;

        Notices = [Service.WriteOffCopies(InventoryBookId, InventoryQuantity, InventoryReason)];
        ClearInventoryForm();
        Refresh();
    }

    [Command]
    public void RelocateBook()
    {
        if (!ValidateInventoryAction(requireLocation: true))
            return;

        Notices = [Service.RelocateBook(InventoryBookId, InventoryLocation, InventoryReason)];
        ClearInventoryForm();
        Refresh();
    }

    [Command]
    public void ExportBooks()
    {
        ImportExportCsv = Service.ExportBooksCsv(Query, FilterStatus, FilterCategory);
        Notices = [new AdminNotice("Exported current catalog filter to CSV.", "green")];
        Refresh();
    }

    [Command]
    public void ImportBooks()
    {
        if (!EnsureText(ImportExportCsv, nameof(ImportExportCsv), "Paste CSV content before importing."))
            return;

        Notices = [Service.ImportBooksCsv(ImportExportCsv)];
        Refresh();
    }

    [Command]
    public void ClearImportExport()
    {
        ImportExportCsv = string.Empty;
        Notices = [new AdminNotice("Cleared CSV import/export workspace.", "blue")];
        Refresh();
    }

    private LibraryService Service => Ctx.Services.GetRequiredService<LibraryService>();

    private bool ValidateAddBook()
    {
        var valid = true;
        valid &= EnsureText(BookIsbn, nameof(BookIsbn), "ISBN is required.");
        valid &= EnsureText(BookTitle, nameof(BookTitle), "Title is required.");
        valid &= EnsureText(BookAuthor, nameof(BookAuthor), "Author is required.");
        valid &= EnsureText(BookCategory, nameof(BookCategory), "Category is required.");
        valid &= EnsureText(BookLocation, nameof(BookLocation), "Location is required.");
        valid &= EnsurePositive(BookPublishedYear, nameof(BookPublishedYear), "Published year is required.");
        valid &= EnsurePositive(BookCopyCount, nameof(BookCopyCount), "Copy count must be greater than zero.");
        return valid;
    }

    private bool ValidateUpdateBook()
    {
        var valid = EnsurePositive(BookId, nameof(BookId), "Book Id is required.");
        valid &= ValidateAddBook();
        return valid;
    }

    private bool ValidateReaderProfile(bool includeReaderId)
    {
        var valid = true;
        if (includeReaderId)
            valid &= EnsurePositive(ReaderId, nameof(ReaderId), "Reader Id is required.");

        valid &= EnsureText(ReaderName, nameof(ReaderName), "Reader name is required.");
        valid &= EnsureText(ReaderEmail, nameof(ReaderEmail), "Reader email is required.");
        valid &= EnsureText(ReaderPhone, nameof(ReaderPhone), "Reader phone is required.");
        valid &= EnsureText(ReaderLevel, nameof(ReaderLevel), "Reader level is required.");
        return valid;
    }

    private bool ValidateLoanPair()
    {
        var valid = true;
        valid &= EnsurePositive(BookId, nameof(BookId), "Book Id is required.");
        valid &= EnsurePositive(ParseId(LoanReaderId), nameof(LoanReaderId), "Reader Id is required.");
        return valid;
    }

    private bool ValidateReservationPair()
    {
        var valid = true;
        valid &= EnsurePositive(BookId, nameof(BookId), "Book Id is required.");
        valid &= EnsurePositive(ParseId(ReservationReaderId), nameof(ReservationReaderId), "Reader Id is required.");
        return valid;
    }

    private bool ValidateInventoryAction(bool requireLocation)
    {
        var valid = true;
        valid &= EnsurePositive(InventoryBookId, nameof(InventoryBookId), "Book Id is required.");
        valid &= EnsurePositive(InventoryQuantity, nameof(InventoryQuantity), "Quantity must be greater than zero.");
        valid &= EnsureText(InventoryReason, nameof(InventoryReason), "Reason is required.");
        if (requireLocation)
            valid &= EnsureText(InventoryLocation, nameof(InventoryLocation), "Location is required.");

        return valid;
    }

    private bool EnsureText(string? value, string field, string message)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return true;

        RecordValidationError(field, message);
        return false;
    }

    private bool EnsurePositive(int value, string field, string message)
    {
        if (value > 0)
            return true;

        RecordValidationError(field, message);
        return false;
    }

    private bool EnsurePositive(decimal value, string field, string message)
    {
        if (value > 0)
            return true;

        RecordValidationError(field, message);
        return false;
    }

    private void RecordValidationError(string field, string message)
    {
        Notices = [new AdminNotice($"{field}: {message}", "red")];
    }

    private void Refresh()
    {
        var snapshot = Service.GetSnapshot(Query, FilterStatus, FilterCategory);
        Books = snapshot.Books;
        Readers = snapshot.Readers;
        Loans = snapshot.Loans;
        Reservations = snapshot.Reservations;
        InventoryEvents = snapshot.InventoryEvents;
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

    private void ClearInventoryForm()
    {
        InventoryBookId = 0;
        InventoryQuantity = 1;
        InventoryLocation = string.Empty;
        InventoryReason = string.Empty;
    }

    private static int ParseId(string value) => int.TryParse(value, out var result) ? result : 0;
}
