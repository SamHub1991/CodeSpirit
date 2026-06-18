namespace $safeprojectname$.Models;

public class BookItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = "Available";
    public string Borrower { get; set; } = string.Empty;
    public int PublishedYear { get; set; }
    public int MonthlyBorrows { get; set; }
    public decimal Rating { get; set; }
    public DateTime LastActionAt { get; set; } = DateTime.Today;
}

public record LibraryMetric(string Label, string Value, string Hint, string Tone);

public record CategoryStat(string Name, int Total, int Borrowed, int Available);

public record LibraryActivity(string Time, string Text, string Tone);
