# CodeSpirit

A .NET 10 framework that brings Spring Boot's elegance to .NET, embracing convention over configuration.

> Simple is better than complex. Readability counts.

## Philosophy

CodeSpirit combines Spring Boot's developer experience with .NET's performance:

| Spring Boot | CodeSpirit | .NET Advantage |
|-------------|------------|----------------|
| `@Service` | `[Service]` | Source Generator compile-time registration |
| `@Scheduled` | `[Scheduled]` | Native async/await, no Quartz complexity |
| `@RestTemplate` | `[Http.Get]` | Built-in typed HTTP client |
| `@Value` | `IOptions<T>` | Strong-typed configuration |
| Actuator | `/actuator/health` | Native HealthChecks |

## Quick Start

```csharp
// Program.cs - 3 lines to start
var builder = WebApplication.CreateBuilder(args);
builder.AddCodeSpirit();
var app = builder.Build();
app.UseCodeSpirit();
app.Run();
```

## Features

### 1. Service Registration

```csharp
[Service]  // Auto-registered as Scoped
public class UserService : IUserService
{
    public async Task<User> GetAsync(Guid id) => ...;
}
```

### 2. Scheduled Tasks

```csharp
[Service]
public class Jobs
{
    // Cron expression
    [Scheduled("0 */5 * * * ?")]
    public async Task SyncDataAsync() { }

    // Simple interval (seconds)
    [Every(60)]
    public async Task HealthCheckAsync() { }

    // Run on startup
    [OnStartup(2000)]  // 2 second delay
    public async Task WarmCacheAsync() { }
}
```

### 3. HTTP Client

```csharp
[Service]
public class ApiClient(IHttp http)
{
    public async Task<List<User>> GetUsersAsync()
    {
        return await http.Get<List<User>>("https://api.example.com/users");
    }
}
```

Or annotate methods for auto-routing:

```csharp
[Service]
public class UserApi
{
    [HttpGet("https://api.example.com/users")]
    public async Task<List<User>> FetchUsers() { }
}
```

### 4. MVVM ViewModels

```csharp
[Page("/customers")]  // Route convention
[Service]
public class CustomerViewModel : ViewModel
{
    [FromQuery] public string? Search { get; set; }
    [Bind] public List<Customer> Customers { get; set; } = [];

    public override Task LoadAsync()
    {
        Customers = FetchCustomers(Search);
        return Task.CompletedTask;
    }
}
```

Access via `GET /customers?Search=alice` - returns ViewModel state as JSON.

### 5. Repository Pattern

```csharp
// Entity with audit fields
public class Order : IHasAuditFields
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Usage
[Service]
public class OrderService(IRepository<Order> orders)
{
    public async Task<Order> CreateAsync(Order order)
    {
        return await orders.AddAsync(order);
    }
}
```

### 6. Module System

```csharp
[Require(typeof(RedisCache))]  // Conditional on class available
public class CachingModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext ctx)
    {
        ctx.Services.AddRedisCache(ctx.Configuration);
    }
}
```

## Project Structure

```
src/
├── CodeSpirit.Core/           # Core abstractions & attributes
│   ├── ServiceAttribute.cs
│   ├── ScheduledAttribute.cs
│   ├── HttpAttribute.cs
│   ├── ViewModel.cs
│   └── Assemblies.cs          # Assembly discovery
├── CodeSpirit.Infrastructure/ # Implementations
│   ├── Scheduling/            # Quartz integration
│   ├── Http/                  # HTTP client
│   ├── EntityFramework/       # Repository pattern
│   └── Mvvm/                  # ViewModel executor
├── CodeSpirit.Modules/        # Example modules
│   ├── UserManagement/
│   └── Demo/                  # Scheduled jobs demo
└── CodeSpirit.Host/           # Entry point
```

## Configuration

```json
{
  "CodeSpirit": {
    "Name": "MyApp",
    "Profile": "development"
  },
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "CodeSpirit",
    "Audience": "MyApp"
  },
  "Database": {
    "Provider": "Sqlite",
    "Name": "app.db"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

## Building

```bash
dotnet build src/CodeSpirit.slnx
dotnet run --project src/CodeSpirit.Host
```

## License

MIT
