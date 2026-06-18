using CodeSpirit.Web.Models;
using ServiceAttribute = CodeSpirit.Core.Attributes.ServiceAttribute;
using CodeSpiritServiceLifetime = CodeSpirit.Core.Attributes.ServiceLifetime;

namespace CodeSpirit.Web.Services;

[Service(Lifetime = CodeSpiritServiceLifetime.Singleton)]
public class LibraryService
{
    private readonly object _sync = new();
    private readonly List<BookItem> _books =
    [
        new() { Id = 1, Title = "Clean Architecture", Author = "Robert C. Martin", Category = "Software", Location = "A-01", Status = "Borrowed", Borrower = "Alice", PublishedYear = 2017, MonthlyBorrows = 42, Rating = 4.8m, LastActionAt = DateTime.Today.AddHours(-2) },
        new() { Id = 2, Title = "Designing Data-Intensive Applications", Author = "Martin Kleppmann", Category = "Software", Location = "A-02", Status = "Available", PublishedYear = 2017, MonthlyBorrows = 38, Rating = 4.9m, LastActionAt = DateTime.Today.AddHours(-5) },
        new() { Id = 3, Title = "The Pragmatic Programmer", Author = "David Thomas", Category = "Software", Location = "A-03", Status = "Borrowed", Borrower = "Bob", PublishedYear = 1999, MonthlyBorrows = 35, Rating = 4.7m, LastActionAt = DateTime.Today.AddDays(-1) },
        new() { Id = 4, Title = "Atomic Habits", Author = "James Clear", Category = "Management", Location = "B-11", Status = "Available", PublishedYear = 2018, MonthlyBorrows = 29, Rating = 4.6m, LastActionAt = DateTime.Today.AddDays(-1) },
        new() { Id = 5, Title = "Thinking, Fast and Slow", Author = "Daniel Kahneman", Category = "Psychology", Location = "C-08", Status = "Borrowed", Borrower = "Chen", PublishedYear = 2011, MonthlyBorrows = 31, Rating = 4.5m, LastActionAt = DateTime.Today.AddDays(-2) },
        new() { Id = 6, Title = "Creative Confidence", Author = "Tom Kelley", Category = "Innovation", Location = "D-20", Status = "Available", PublishedYear = 2013, MonthlyBorrows = 24, Rating = 4.4m, LastActionAt = DateTime.Today.AddDays(-3) },
        new() { Id = 7, Title = "Deep Work", Author = "Cal Newport", Category = "Management", Location = "B-12", Status = "Available", PublishedYear = 2016, MonthlyBorrows = 27, Rating = 4.6m, LastActionAt = DateTime.Today.AddDays(-3) },
        new() { Id = 8, Title = "Educated", Author = "Tara Westover", Category = "Biography", Location = "E-04", Status = "Borrowed", Borrower = "Diana", PublishedYear = 2018, MonthlyBorrows = 19, Rating = 4.3m, LastActionAt = DateTime.Today.AddDays(-4) }
    ];

    private readonly List<LibraryActivity> _activities =
    [
        new("09:30", "Alice borrowed Clean Architecture", "blue"),
        new("10:20", "Designing Data-Intensive Applications returned to A-02", "green"),
        new("11:05", "New reservation created for The Pragmatic Programmer", "amber"),
        new("13:40", "Deep Work moved to featured shelf", "purple")
    ];

    public LibrarySnapshot GetSnapshot()
    {
        lock (_sync)
        {
            var books = _books.Select(Clone).ToList();
            var borrowed = books.Count(book => book.Status == "Borrowed");
            var available = books.Count(book => book.Status == "Available");
            var totalBorrows = books.Sum(book => book.MonthlyBorrows);

            return new LibrarySnapshot(
                books,
                [
                    new("Total Books", books.Count.ToString(), "Live collection size", "blue"),
                    new("Available", available.ToString(), "Ready for readers", "green"),
                    new("Borrowed", borrowed.ToString(), "Currently checked out", "amber"),
                    new("Monthly Borrows", totalBorrows.ToString(), "Across all categories", "purple")
                ],
                books.GroupBy(book => book.Category)
                    .Select(group => new CategoryStat(group.Key, group.Count(), group.Count(book => book.Status == "Borrowed"), group.Count(book => book.Status == "Available")))
                    .OrderByDescending(stat => stat.Total)
                    .ToList(),
                books.OrderByDescending(book => book.MonthlyBorrows).Take(5).ToList(),
                _activities.Take(6).ToList());
        }
    }

    public void AddBook(string title, string author, string category, string location)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author))
            return;

        lock (_sync)
        {
            var nextId = _books.Count == 0 ? 1 : _books.Max(book => book.Id) + 1;
            _books.Add(new BookItem
            {
                Id = nextId,
                Title = title.Trim(),
                Author = author.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
                Location = string.IsNullOrWhiteSpace(location) ? "New Shelf" : location.Trim(),
                PublishedYear = DateTime.Today.Year,
                MonthlyBorrows = 0,
                Rating = 4.0m,
                LastActionAt = DateTime.Now
            });

            AddActivity($"New book added: {title.Trim()}", "green");
        }
    }

    public void BorrowBook(int bookId, string borrower)
    {
        lock (_sync)
        {
            var book = _books.FirstOrDefault(item => item.Id == bookId && item.Status == "Available");
            if (book is null)
                return;

            book.Status = "Borrowed";
            book.Borrower = string.IsNullOrWhiteSpace(borrower) ? "Guest Reader" : borrower.Trim();
            book.MonthlyBorrows++;
            book.LastActionAt = DateTime.Now;
            AddActivity($"{book.Borrower} borrowed {book.Title}", "blue");
        }
    }

    public void ReturnBook(int bookId)
    {
        lock (_sync)
        {
            var book = _books.FirstOrDefault(item => item.Id == bookId && item.Status == "Borrowed");
            if (book is null)
                return;

            var title = book.Title;
            book.Status = "Available";
            book.Borrower = string.Empty;
            book.LastActionAt = DateTime.Now;
            AddActivity($"{title} returned to {book.Location}", "green");
        }
    }

    private void AddActivity(string text, string tone)
    {
        _activities.Insert(0, new LibraryActivity(DateTime.Now.ToString("HH:mm"), text, tone));
    }

    private static BookItem Clone(BookItem book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Category = book.Category,
        Location = book.Location,
        Status = book.Status,
        Borrower = book.Borrower,
        PublishedYear = book.PublishedYear,
        MonthlyBorrows = book.MonthlyBorrows,
        Rating = book.Rating,
        LastActionAt = book.LastActionAt
    };
}

public record LibrarySnapshot(
    List<BookItem> Books,
    List<LibraryMetric> Metrics,
    List<CategoryStat> CategoryStats,
    List<BookItem> PopularBooks,
    List<LibraryActivity> Activities);
