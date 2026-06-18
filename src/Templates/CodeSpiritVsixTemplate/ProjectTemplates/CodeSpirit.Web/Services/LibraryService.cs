using CodeSpirit.Web.Models;
using ServiceAttribute = CodeSpirit.Core.Attributes.ServiceAttribute;
using CodeSpiritServiceLifetime = CodeSpirit.Core.Attributes.ServiceLifetime;

namespace CodeSpirit.Web.Services;

[Service(Lifetime = CodeSpiritServiceLifetime.Singleton)]
public class LibraryService
{
    private readonly object _sync = new();
    private readonly List<BookItem> _books = [];
    private readonly List<ReaderItem> _readers = [];
    private readonly List<LoanRecord> _loans = [];
    private readonly List<ReservationItem> _reservations = [];
    private readonly List<LibraryActivity> _activities = [];
    private int _nextBookId = 1;
    private int _nextReaderId = 1;
    private int _nextLoanId = 1;
    private int _nextReservationId = 1;

    public LibraryService()
    {
        Seed();
    }

    public LibrarySnapshot GetSnapshot(string query = "", string status = "All", string category = "All")
    {
        lock (_sync)
        {
            RecalculateBooks();
            RecalculateReaders();

            var books = FilterBooks(query, status, category).Select(Clone).ToList();
            var readers = _readers.Select(Clone).ToList();
            var loans = _loans.OrderByDescending(loan => loan.Id).Select(Clone).ToList();
            var reservations = _reservations.OrderByDescending(item => item.Id).Select(Clone).ToList();
            var totalCopies = _books.Sum(book => book.CopyCount);
            var availableCopies = _books.Sum(book => book.AvailableCopies);
            var activeLoans = _loans.Count(loan => loan.Status is "Active" or "Overdue");
            var overdueLoans = _loans.Count(loan => loan.Status == "Overdue");
            var waitingReservations = _reservations.Count(item => item.Status == "Waiting");
            var fines = _readers.Sum(reader => reader.FineBalance);

            return new LibrarySnapshot(
                books,
                readers,
                loans,
                reservations,
                [
                    new("Books", _books.Count.ToString(), $"{totalCopies} total copies", "blue"),
                    new("Available", availableCopies.ToString(), "Copies ready to borrow", "green"),
                    new("Active Loans", activeLoans.ToString(), $"{overdueLoans} overdue", overdueLoans > 0 ? "red" : "amber"),
                    new("Reservations", waitingReservations.ToString(), $"Fines ${fines:0.00}", "purple")
                ],
                _books.GroupBy(book => book.Category)
                    .Select(group => new CategoryStat(group.Key, group.Count(), group.Count(book => book.Status == "Borrowed"), group.Sum(book => book.AvailableCopies)))
                    .OrderByDescending(stat => stat.Total)
                    .ToList(),
                _books.OrderByDescending(book => book.MonthlyBorrows).Take(5).Select(Clone).ToList(),
                _activities.Take(8).ToList());
        }
    }

    public AdminNotice AddBook(string isbn, string title, string author, string category, string location, int publishedYear, int copyCount)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author))
            return new("Title and author are required.", "red");

        lock (_sync)
        {
            var copies = Math.Max(1, copyCount);
            var book = new BookItem
            {
                Id = _nextBookId++,
                Isbn = string.IsNullOrWhiteSpace(isbn) ? $"ISBN-{DateTime.Now:yyyyMMddHHmmss}" : isbn.Trim(),
                Title = title.Trim(),
                Author = author.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
                Location = string.IsNullOrWhiteSpace(location) ? "New Shelf" : location.Trim(),
                CopyCount = copies,
                AvailableCopies = copies,
                PublishedYear = publishedYear <= 0 ? DateTime.Today.Year : publishedYear,
                MonthlyBorrows = 0,
                Rating = 4.0m,
                LastActionAt = DateTime.Now
            };
            _books.Add(book);
            RecalculateBooks();
            AddActivity($"New book added: {book.Title}", "green");
            return new($"Added {book.Title} with {copies} copies.", "green");
        }
    }

    public AdminNotice UpdateBook(int bookId, string isbn, string title, string author, string category, string location, int publishedYear, int copyCount)
    {
        lock (_sync)
        {
            var book = _books.FirstOrDefault(item => item.Id == bookId);
            if (book is null)
                return new("Book was not found.", "red");

            var activeLoans = ActiveLoansFor(bookId).Count();
            book.Isbn = string.IsNullOrWhiteSpace(isbn) ? book.Isbn : isbn.Trim();
            book.Title = string.IsNullOrWhiteSpace(title) ? book.Title : title.Trim();
            book.Author = string.IsNullOrWhiteSpace(author) ? book.Author : author.Trim();
            book.Category = string.IsNullOrWhiteSpace(category) ? book.Category : category.Trim();
            book.Location = string.IsNullOrWhiteSpace(location) ? book.Location : location.Trim();
            book.PublishedYear = publishedYear <= 0 ? book.PublishedYear : publishedYear;
            book.CopyCount = Math.Max(activeLoans, copyCount <= 0 ? book.CopyCount : copyCount);
            book.LastActionAt = DateTime.Now;
            RecalculateBooks();
            SyncLoanBookTitles(book);
            SyncReservationBookTitles(book);
            AddActivity($"Book updated: {book.Title}", "blue");
            return new($"Updated {book.Title}.", "blue");
        }
    }

    public AdminNotice ArchiveBook(int bookId)
    {
        lock (_sync)
        {
            var book = _books.FirstOrDefault(item => item.Id == bookId);
            if (book is null)
                return new("Book was not found.", "red");

            if (ActiveLoansFor(bookId).Any())
                return new("Return all active loans before archiving this book.", "amber");

            book.Status = "Archived";
            book.AvailableCopies = 0;
            book.LastActionAt = DateTime.Now;
            CancelReservationsForBook(bookId);
            AddActivity($"Book archived: {book.Title}", "purple");
            return new($"Archived {book.Title}.", "purple");
        }
    }

    public AdminNotice RestoreBook(int bookId)
    {
        lock (_sync)
        {
            var book = _books.FirstOrDefault(item => item.Id == bookId);
            if (book is null)
                return new("Book was not found.", "red");

            book.Status = "Available";
            book.LastActionAt = DateTime.Now;
            RecalculateBooks();
            AddActivity($"Book restored: {book.Title}", "green");
            return new($"Restored {book.Title}.", "green");
        }
    }

    public AdminNotice RegisterReader(string name, string email, string phone, string level)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new("Reader name is required.", "red");

        lock (_sync)
        {
            var reader = new ReaderItem
            {
                Id = _nextReaderId++,
                Name = name.Trim(),
                Email = email.Trim(),
                Phone = phone.Trim(),
                Level = string.IsNullOrWhiteSpace(level) ? "Standard" : level.Trim(),
                Status = "Active"
            };
            _readers.Add(reader);
            AddActivity($"Reader registered: {reader.Name}", "green");
            return new($"Registered reader {reader.Name}.", "green");
        }
    }

    public AdminNotice UpdateReader(int readerId, string name, string email, string phone, string level)
    {
        lock (_sync)
        {
            var reader = _readers.FirstOrDefault(item => item.Id == readerId);
            if (reader is null)
                return new("Reader was not found.", "red");

            reader.Name = string.IsNullOrWhiteSpace(name) ? reader.Name : name.Trim();
            reader.Email = string.IsNullOrWhiteSpace(email) ? reader.Email : email.Trim();
            reader.Phone = string.IsNullOrWhiteSpace(phone) ? reader.Phone : phone.Trim();
            reader.Level = string.IsNullOrWhiteSpace(level) ? reader.Level : level.Trim();
            SyncLoanReaderNames(reader);
            SyncReservationReaderNames(reader);
            AddActivity($"Reader updated: {reader.Name}", "blue");
            return new($"Updated reader {reader.Name}.", "blue");
        }
    }

    public AdminNotice SuspendReader(int readerId)
    {
        lock (_sync)
        {
            var reader = _readers.FirstOrDefault(item => item.Id == readerId);
            if (reader is null)
                return new("Reader was not found.", "red");

            if (_loans.Any(loan => loan.ReaderId == readerId && loan.Status is "Active" or "Overdue"))
                return new("Return active loans before suspending this reader.", "amber");

            reader.Status = "Suspended";
            CancelReservationsForReader(readerId);
            AddActivity($"Reader suspended: {reader.Name}", "purple");
            return new($"Suspended reader {reader.Name}.", "purple");
        }
    }

    public AdminNotice ActivateReader(int readerId)
    {
        lock (_sync)
        {
            var reader = _readers.FirstOrDefault(item => item.Id == readerId);
            if (reader is null)
                return new("Reader was not found.", "red");

            reader.Status = "Active";
            AddActivity($"Reader activated: {reader.Name}", "green");
            return new($"Activated reader {reader.Name}.", "green");
        }
    }

    public AdminNotice BorrowBook(int bookId, int readerId)
    {
        lock (_sync)
        {
            var book = _books.FirstOrDefault(item => item.Id == bookId);
            var reader = _readers.FirstOrDefault(item => item.Id == readerId);
            if (book is null)
                return new("Book was not found.", "red");
            if (reader is null)
                return new("Reader was not found.", "red");
            if (reader.Status != "Active")
                return new("Reader must be active before borrowing.", "amber");
            if (book.Status == "Archived")
                return new("Archived books cannot be borrowed.", "amber");
            if (book.AvailableCopies <= 0)
                return new("No available copies. Create a reservation instead.", "amber");
            if (_loans.Any(loan => loan.BookId == bookId && loan.ReaderId == readerId && loan.Status is "Active" or "Overdue"))
                return new("Reader already has an active loan for this book.", "amber");

            var dueAt = DateTime.Today.AddDays(GetLoanDays(reader.Level));
            var loan = new LoanRecord
            {
                Id = _nextLoanId++,
                BookId = book.Id,
                BookTitle = book.Title,
                ReaderId = reader.Id,
                ReaderName = reader.Name,
                BorrowedAt = DateTime.Today.ToString("yyyy-MM-dd"),
                DueAt = dueAt.ToString("yyyy-MM-dd"),
                Status = "Active"
            };
            _loans.Add(loan);
            book.MonthlyBorrows++;
            book.LastActionAt = DateTime.Now;
            CompleteReservation(bookId, readerId);
            RecalculateBooks();
            RecalculateReaders();
            AddActivity($"{reader.Name} borrowed {book.Title}", "blue");
            return new($"Borrowed {book.Title} to {reader.Name}, due {loan.DueAt}.", "blue");
        }
    }

    public AdminNotice ReturnBook(int loanId)
    {
        lock (_sync)
        {
            var loan = _loans.FirstOrDefault(item => item.Id == loanId && item.Status is "Active" or "Overdue");
            if (loan is null)
                return new("Active loan was not found.", "red");

            var fine = CalculateFine(loan);
            loan.Status = "Returned";
            loan.ReturnedAt = DateTime.Today.ToString("yyyy-MM-dd");
            loan.Fine = fine;
            var reader = _readers.FirstOrDefault(item => item.Id == loan.ReaderId);
            if (reader is not null)
                reader.FineBalance += fine;

            var book = _books.FirstOrDefault(item => item.Id == loan.BookId);
            if (book is not null)
                book.LastActionAt = DateTime.Now;

            RecalculateBooks();
            RecalculateReaders();
            AddActivity($"{loan.BookTitle} returned by {loan.ReaderName}", fine > 0 ? "amber" : "green");
            return new(fine > 0 ? $"Returned with ${fine:0.00} fine." : $"Returned {loan.BookTitle}.", fine > 0 ? "amber" : "green");
        }
    }

    public AdminNotice RenewLoan(int loanId)
    {
        lock (_sync)
        {
            var loan = _loans.FirstOrDefault(item => item.Id == loanId && item.Status is "Active" or "Overdue");
            if (loan is null)
                return new("Active loan was not found.", "red");
            if (loan.RenewCount >= 2)
                return new("This loan has reached the renewal limit.", "amber");
            if (_reservations.Any(item => item.BookId == loan.BookId && item.Status == "Waiting" && item.ReaderId != loan.ReaderId))
                return new("This book has waiting reservations and cannot be renewed.", "amber");

            var due = DateTime.Parse(loan.DueAt).AddDays(14);
            loan.DueAt = due.ToString("yyyy-MM-dd");
            loan.RenewCount++;
            loan.Status = due < DateTime.Today ? "Overdue" : "Active";
            AddActivity($"Loan renewed: {loan.BookTitle} for {loan.ReaderName}", "green");
            return new($"Renewed until {loan.DueAt}.", "green");
        }
    }

    public AdminNotice ReserveBook(int bookId, int readerId)
    {
        lock (_sync)
        {
            var book = _books.FirstOrDefault(item => item.Id == bookId);
            var reader = _readers.FirstOrDefault(item => item.Id == readerId);
            if (book is null)
                return new("Book was not found.", "red");
            if (reader is null)
                return new("Reader was not found.", "red");
            if (book.Status == "Archived" || reader.Status != "Active")
                return new("Only active readers can reserve active books.", "amber");
            if (_reservations.Any(item => item.BookId == bookId && item.ReaderId == readerId && item.Status == "Waiting"))
                return new("Reader already has a waiting reservation for this book.", "amber");

            var reservation = new ReservationItem
            {
                Id = _nextReservationId++,
                BookId = book.Id,
                BookTitle = book.Title,
                ReaderId = reader.Id,
                ReaderName = reader.Name,
                CreatedAt = DateTime.Today.ToString("yyyy-MM-dd"),
                Status = "Waiting"
            };
            _reservations.Add(reservation);
            RecalculateBooks();
            RecalculateReaders();
            AddActivity($"{reader.Name} reserved {book.Title}", "amber");
            return new($"Reserved {book.Title} for {reader.Name}.", "amber");
        }
    }

    public AdminNotice CancelReservation(int reservationId)
    {
        lock (_sync)
        {
            var reservation = _reservations.FirstOrDefault(item => item.Id == reservationId && item.Status == "Waiting");
            if (reservation is null)
                return new("Waiting reservation was not found.", "red");

            reservation.Status = "Cancelled";
            RecalculateBooks();
            RecalculateReaders();
            AddActivity($"Reservation cancelled: {reservation.BookTitle} for {reservation.ReaderName}", "purple");
            return new("Reservation cancelled.", "purple");
        }
    }

    public AdminNotice CollectFine(int readerId, decimal amount)
    {
        lock (_sync)
        {
            var reader = _readers.FirstOrDefault(item => item.Id == readerId);
            if (reader is null)
                return new("Reader was not found.", "red");

            var paid = amount <= 0 ? reader.FineBalance : Math.Min(reader.FineBalance, amount);
            reader.FineBalance -= paid;
            AddActivity($"Fine collected from {reader.Name}: ${paid:0.00}", "green");
            return new($"Collected ${paid:0.00} from {reader.Name}.", "green");
        }
    }

    private IEnumerable<BookItem> FilterBooks(string query, string status, string category)
    {
        var books = _books.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            books = books.Where(book => book.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || book.Author.Contains(term, StringComparison.OrdinalIgnoreCase)
                || book.Isbn.Contains(term, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            books = books.Where(book => book.Status == status);
        if (!string.IsNullOrWhiteSpace(category) && category != "All")
            books = books.Where(book => book.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        return books.OrderBy(book => book.Title);
    }

    private void Seed()
    {
        _readers.AddRange([
            new() { Id = _nextReaderId++, Name = "Alice", Email = "alice@example.com", Phone = "13800000001", Level = "Premium" },
            new() { Id = _nextReaderId++, Name = "Bob", Email = "bob@example.com", Phone = "13800000002", Level = "Standard" },
            new() { Id = _nextReaderId++, Name = "Chen", Email = "chen@example.com", Phone = "13800000003", Level = "Standard" },
            new() { Id = _nextReaderId++, Name = "Diana", Email = "diana@example.com", Phone = "13800000004", Level = "Student" }
        ]);

        AddSeedBook("9780134494166", "Clean Architecture", "Robert C. Martin", "Software", "A-01", 2017, 3, 42, 4.8m);
        AddSeedBook("9781449373320", "Designing Data-Intensive Applications", "Martin Kleppmann", "Software", "A-02", 2017, 2, 38, 4.9m);
        AddSeedBook("9780201616224", "The Pragmatic Programmer", "David Thomas", "Software", "A-03", 1999, 2, 35, 4.7m);
        AddSeedBook("9780735211292", "Atomic Habits", "James Clear", "Management", "B-11", 2018, 4, 29, 4.6m);
        AddSeedBook("9780374533557", "Thinking, Fast and Slow", "Daniel Kahneman", "Psychology", "C-08", 2011, 2, 31, 4.5m);
        AddSeedBook("9780385349369", "Creative Confidence", "Tom Kelley", "Innovation", "D-20", 2013, 1, 24, 4.4m);
        AddSeedBook("9781455586691", "Deep Work", "Cal Newport", "Management", "B-12", 2016, 3, 27, 4.6m);
        AddSeedBook("9780399590504", "Educated", "Tara Westover", "Biography", "E-04", 2018, 1, 19, 4.3m);

        BorrowBook(1, 1);
        BorrowBook(3, 2);
        BorrowBook(5, 3);
        BorrowBook(8, 4);
        ReserveBook(3, 1);
        _activities.Clear();
        _activities.AddRange([
            new("09:30", "Alice borrowed Clean Architecture", "blue"),
            new("10:20", "Designing Data-Intensive Applications returned to A-02", "green"),
            new("11:05", "Alice reserved The Pragmatic Programmer", "amber"),
            new("13:40", "Deep Work moved to featured shelf", "purple")
        ]);
        RecalculateBooks();
        RecalculateReaders();
    }

    private void AddSeedBook(string isbn, string title, string author, string category, string location, int year, int copies, int borrows, decimal rating)
    {
        _books.Add(new BookItem
        {
            Id = _nextBookId++,
            Isbn = isbn,
            Title = title,
            Author = author,
            Category = category,
            Location = location,
            CopyCount = copies,
            AvailableCopies = copies,
            PublishedYear = year,
            MonthlyBorrows = borrows,
            Rating = rating,
            LastActionAt = DateTime.Today.AddDays(-1)
        });
    }

    private void RecalculateBooks()
    {
        foreach (var loan in _loans.Where(item => item.Status is "Active" or "Overdue"))
        {
            if (DateTime.Parse(loan.DueAt) < DateTime.Today)
                loan.Status = "Overdue";
        }

        foreach (var book in _books)
        {
            if (book.Status == "Archived")
            {
                book.AvailableCopies = 0;
                book.StatusTone = "purple";
                continue;
            }

            var activeLoans = ActiveLoansFor(book.Id).Count();
            book.AvailableCopies = Math.Max(0, book.CopyCount - activeLoans);
            var hasWaiting = _reservations.Any(item => item.BookId == book.Id && item.Status == "Waiting");
            var currentLoan = _loans.LastOrDefault(item => item.BookId == book.Id && item.Status is "Active" or "Overdue");
            book.Borrower = currentLoan?.ReaderName ?? string.Empty;
            book.DueDate = currentLoan?.DueAt ?? string.Empty;
            book.ReservedBy = _reservations.FirstOrDefault(item => item.BookId == book.Id && item.Status == "Waiting")?.ReaderName ?? string.Empty;
            book.Status = currentLoan?.Status == "Overdue" ? "Overdue" : book.AvailableCopies > 0 ? "Available" : hasWaiting ? "Reserved" : "Borrowed";
            book.StatusTone = book.Status switch
            {
                "Available" => "green",
                "Borrowed" => "amber",
                "Reserved" => "blue",
                "Overdue" => "red",
                _ => "purple"
            };
        }
    }

    private void RecalculateReaders()
    {
        foreach (var reader in _readers)
        {
            reader.ActiveLoans = _loans.Count(loan => loan.ReaderId == reader.Id && loan.Status is "Active" or "Overdue");
            reader.Reservations = _reservations.Count(item => item.ReaderId == reader.Id && item.Status == "Waiting");
        }
    }

    private IEnumerable<LoanRecord> ActiveLoansFor(int bookId) => _loans.Where(loan => loan.BookId == bookId && loan.Status is "Active" or "Overdue");

    private static int GetLoanDays(string level) => level switch
    {
        "Premium" => 45,
        "Student" => 35,
        _ => 30
    };

    private static decimal CalculateFine(LoanRecord loan)
    {
        var overdueDays = (DateTime.Today - DateTime.Parse(loan.DueAt)).Days;
        return overdueDays > 0 ? overdueDays * 0.5m : 0m;
    }

    private void CompleteReservation(int bookId, int readerId)
    {
        var reservation = _reservations.FirstOrDefault(item => item.BookId == bookId && item.ReaderId == readerId && item.Status == "Waiting");
        if (reservation is not null)
            reservation.Status = "Completed";
    }

    private void CancelReservationsForBook(int bookId)
    {
        foreach (var item in _reservations.Where(item => item.BookId == bookId && item.Status == "Waiting"))
            item.Status = "Cancelled";
    }

    private void CancelReservationsForReader(int readerId)
    {
        foreach (var item in _reservations.Where(item => item.ReaderId == readerId && item.Status == "Waiting"))
            item.Status = "Cancelled";
    }

    private void SyncLoanBookTitles(BookItem book)
    {
        foreach (var loan in _loans.Where(item => item.BookId == book.Id))
            loan.BookTitle = book.Title;
    }

    private void SyncReservationBookTitles(BookItem book)
    {
        foreach (var item in _reservations.Where(item => item.BookId == book.Id))
            item.BookTitle = book.Title;
    }

    private void SyncLoanReaderNames(ReaderItem reader)
    {
        foreach (var loan in _loans.Where(item => item.ReaderId == reader.Id))
            loan.ReaderName = reader.Name;
    }

    private void SyncReservationReaderNames(ReaderItem reader)
    {
        foreach (var item in _reservations.Where(item => item.ReaderId == reader.Id))
            item.ReaderName = reader.Name;
    }

    private void AddActivity(string text, string tone)
    {
        _activities.Insert(0, new LibraryActivity(DateTime.Now.ToString("HH:mm"), text, tone));
    }

    private static BookItem Clone(BookItem book) => new()
    {
        Id = book.Id,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Category = book.Category,
        Location = book.Location,
        Status = book.Status,
        StatusTone = book.StatusTone,
        CopyCount = book.CopyCount,
        AvailableCopies = book.AvailableCopies,
        Borrower = book.Borrower,
        ReservedBy = book.ReservedBy,
        DueDate = book.DueDate,
        PublishedYear = book.PublishedYear,
        MonthlyBorrows = book.MonthlyBorrows,
        Rating = book.Rating,
        LastActionAt = book.LastActionAt
    };

    private static ReaderItem Clone(ReaderItem reader) => new()
    {
        Id = reader.Id,
        Name = reader.Name,
        Email = reader.Email,
        Phone = reader.Phone,
        Level = reader.Level,
        Status = reader.Status,
        ActiveLoans = reader.ActiveLoans,
        Reservations = reader.Reservations,
        FineBalance = reader.FineBalance
    };

    private static LoanRecord Clone(LoanRecord loan) => new()
    {
        Id = loan.Id,
        BookId = loan.BookId,
        BookTitle = loan.BookTitle,
        ReaderId = loan.ReaderId,
        ReaderName = loan.ReaderName,
        BorrowedAt = loan.BorrowedAt,
        DueAt = loan.DueAt,
        ReturnedAt = loan.ReturnedAt,
        Status = loan.Status,
        RenewCount = loan.RenewCount,
        Fine = loan.Fine
    };

    private static ReservationItem Clone(ReservationItem item) => new()
    {
        Id = item.Id,
        BookId = item.BookId,
        BookTitle = item.BookTitle,
        ReaderId = item.ReaderId,
        ReaderName = item.ReaderName,
        CreatedAt = item.CreatedAt,
        Status = item.Status
    };
}

public record LibrarySnapshot(
    List<BookItem> Books,
    List<ReaderItem> Readers,
    List<LoanRecord> Loans,
    List<ReservationItem> Reservations,
    List<LibraryMetric> Metrics,
    List<CategoryStat> CategoryStats,
    List<BookItem> PopularBooks,
    List<LibraryActivity> Activities);
