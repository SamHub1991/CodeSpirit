namespace CodeSpirit.LibraryManagement.Features.Library.Models;

public class BookItem
{
    public int Id { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public string StatusTone { get; set; } = "green";
    public int CopyCount { get; set; } = 1;
    public int AvailableCopies { get; set; } = 1;
    public string Borrower { get; set; } = string.Empty;
    public string ReservedBy { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int MonthlyBorrows { get; set; }
    public decimal Rating { get; set; }
    public DateTime LastActionAt { get; set; } = DateTime.Today;
}

public class ReaderItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Level { get; set; } = "Standard";
    public string Status { get; set; } = "Active";
    public int ActiveLoans { get; set; }
    public int Reservations { get; set; }
    public decimal FineBalance { get; set; }
}

public class LoanRecord
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string BorrowedAt { get; set; } = string.Empty;
    public string DueAt { get; set; } = string.Empty;
    public string ReturnedAt { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int RenewCount { get; set; }
    public decimal Fine { get; set; }
}

public class ReservationItem
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int ReaderId { get; set; }
    public string ReaderName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string Status { get; set; } = "Waiting";
}

public class InventoryEvent
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public record LibraryMetric(string Label, string Value, string Hint, string Tone);

public record CategoryStat(string Name, int Total, int Borrowed, int Available);

public record LibraryActivity(string Time, string Text, string Tone);

public record AdminNotice(string Text, string Tone);
