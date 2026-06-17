using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;

namespace CodeSpirit.Modules.CustomerMvvm.ViewModels;

/// <summary>
/// Simple customer list ViewModel demonstrating MVVM pattern.
/// Convention: Route /customers -> CustomerViewModel
/// </summary>
[PageDirective(Route = "/customers", Title = "Customer Management")]
[Service]
public class CustomerViewModel : ViewModel
{
    [FromQuery] public string? Search { get; set; }
    [FromRoute] public Guid? Id { get; set; }
    [Bind] public List<Customer> Customers { get; set; } = [];
    [Bind] public int TotalCount { get; set; }

    public override Task LoadAsync()
    {
        var all = SeedData();
        var filtered = string.IsNullOrWhiteSpace(Search)
            ? all
            : all.Where(c => c.Name.Contains(Search, StringComparison.OrdinalIgnoreCase)).ToList();

        Customers = filtered;
        TotalCount = filtered.Count;
        return Task.CompletedTask;
    }

    private static List<Customer> SeedData() => [
        new(Guid.NewGuid(), "Alice", "alice@example.com"),
        new(Guid.NewGuid(), "Bob", "bob@example.com"),
        new(Guid.NewGuid(), "Charlie", "charlie@example.com"),
        new(Guid.NewGuid(), "Diana", "diana@example.com")
    ];
}

public record Customer(Guid Id, string Name, string Email);
