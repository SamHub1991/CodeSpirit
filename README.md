# CodeSpirit

A .NET 10 framework that brings Spring Boot's developer experience to .NET.

> Convention over configuration. One line to start. Attribute-driven everything.

## Why CodeSpirit

CodeSpirit maps Spring Boot's core concepts to idiomatic .NET 10:

| Spring Boot | CodeSpirit | How |
|-------------|------------|-----|
| `@Service` | `[Service]` | Source Generator compile-time registration |
| `@Scheduled` | `[Scheduled]` | Native async/await, no Quartz complexity |
| `@Autowired` | `[Autowired]` | Property/field injection via DI |
| `@Value` | `[Value("key")]` | Strong-typed config binding |
| `@Transactional` | `[Transactional]` | AOP interceptor via Castle DynamicProxy |
| `@Cacheable` | `[Cacheable]` | Declarable caching with interceptors |
| WPF binding | `[Bind]` + `[Command]` | Property binding and command events for pages |
| Actuator | `/actuator/*` | Built-in health, metrics, info endpoints |
| `@SpringBootApplication` | `[CodeSpiritApplication]` | Auto-config entry point |

## Quick Start

### Option A: Project Template (dotnet new)

```bash
dotnet new install CodeSpirit.Templates
dotnet new codespirit-library -n CodeSpirit.LibraryManagement
cd CodeSpirit.LibraryManagement
dotnet run
```

### Option B: Visual Studio VSIX

1. Build `src/Templates/CodeSpiritVsixTemplate` to produce `.vsix`
2. Double-click the `.vsix` to install
3. In Visual Studio: New Project -> search "CodeSpirit"

### Option C: From Source

```bash
dotnet build src/CodeSpirit.slnx
dotnet run --project src/CodeSpirit.LibraryManagement
```

## Features

### One-Line Bootstrap

```csharp
[CodeSpiritApplication]
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddCodeSpirit();        // auto-config + module scan
        var app = builder.Build();
        app.UseCodeSpirit();            // middleware + routing
        app.Run();
    }
}
```

### Attribute-Driven Services

No manual DI registration. Annotate and go:

```csharp
[Service]  // Scoped by default
public class UserService
{
    [Autowired]
    private ILogger<UserService> _logger = null!;

    [Value("CodeSpirit:Name")]
    public string AppName { get; set; } = string.Empty;
}
```

Lifetime control: `[Service(Lifetime = ServiceLifetime.Singleton)]`.

### MVVM Pages

ViewModels drive routing and state rendering. Access `GET /customers?Search=alice` returns bindable ViewModel state as JSON:

```csharp
[PageDirective(Route = "/customers", Title = "Customers")]
[Service]
public class CustomerViewModel : ViewModel
{
    [FromQuery] public string? Search { get; set; }
    [Bind] public List<Customer> Customers { get; set; } = [];

    public override Task LoadAsync()
    {
        Customers = _repo.Search(Search);
        return Task.CompletedTask;
    }
}
```

### WPF-Style Binding and Commands

`[Bind]` exposes ViewModel properties to the page runtime. Use `BindDirection.TwoWay` to accept POSTed values back into the ViewModel, then invoke `[Command]` methods with `__command`:

```csharp
[PageDirective(Route = "/weather", Title = "Weather Forecast")]
[Service]
public class WeatherViewModel : ViewModel
{
    [FromQuery]
    [Bind(BindDirection.TwoWay)]
    public string? City { get; set; }

    [Bind] public WeatherForecast[] Forecast { get; set; } = [];

    [Command]
    public void Refresh()
    {
        var service = Services.GetRequiredService<WeatherService>();
        Forecast = service.GetForecast();
    }
}
```

```html
<form method="post" data-cs-vm>
  <input name="City" value="{Binding City}" data-cs-bind="City" />
  <button type="submit" data-cs-command="Refresh">Search</button>
</form>
```

The JSON response includes `state`, `bindings`, and `commands`, which gives a frontend runtime enough metadata to wire property binding and command events.

### jQuery and MVVM Boundary

CodeSpirit keeps page state and visual behaviors separated:

| Attribute | Owner | Responsibility |
|-----------|-------|----------------|
| `data-cs-*` | CodeSpirit MVVM runtime | property binding, form submission, command events, ViewModel state updates |
| `data-ui` | jQuery UI behavior layer | widgets, animation, cards, tabs, tooltip, datepicker, third-party plugin setup |

Use MVVM for data and commands:

```html
<form method="post" data-cs-vm>
  <input name="City" value="{Binding City}" data-cs-bind="City" />
  <button type="submit" data-cs-command="Refresh">Search</button>
</form>
```

Use jQuery for visual behaviors:

```html
<div class="card" data-ui="clickable-card">
  <a href="/weather">Weather</a>
</div>
```

Tell MVVM when a jQuery widget changes a bound value:

```javascript
CodeSpirit.input(element);
```

Initialize jQuery behaviors after rendering new HTML:

```javascript
CodeSpirit.refresh(rootElement);
```

Stable browser APIs:

| API | Use case |
|-----|----------|
| `CodeSpirit.input(element)` | notify MVVM that a bound widget changed |
| `CodeSpirit.input(element, name, value)` | notify MVVM with an explicit property and value |
| `CodeSpirit.applyState(root, state)` | apply ViewModel state to `data-cs-bind` elements |
| `CodeSpirit.applyRegions(root, regions)` | apply server-rendered HTML patches to `data-cs-region` elements |
| `CodeSpirit.updateField(root, name, value)` | update one bound field manually |
| `CodeSpirit.mount(root)` | initialize a DOM root after initial render |
| `CodeSpirit.refresh(root)` | initialize a DOM root after dynamic updates |
| `CodeSpirit.ui.register(name, initializer)` | register a reusable `data-ui` behavior |
| `CodeSpirit.ui.ready(elements, name)` | mark initialized UI behavior elements |
| `CodeSpirit.ui.init(root)` | low-level `data-ui` behavior initializer |

The default template includes two separate files:

```text
wwwroot/js/codespirit.runtime.js
wwwroot/js/ui/jquery.behaviors.js
```

The runtime owns `data-cs-*`. The jQuery layer owns `data-ui`. Business data changes should go through `[Bind]` and `[Command]`. UI widgets should notify MVVM with `CodeSpirit.input(element)`, then the runtime updates bound fields and emits `codespirit:changed`. Server responses may include `regions` HTML patches for `cs:Region` blocks. After MVVM returns new HTML, initialize new widgets with `CodeSpirit.refresh(root)`.

Page tags reduce common MVVM markup:

```html
<cs:Form class="search-card">
  <cs:Field Name="Query" Label="Search" Placeholder="Title, author, or ISBN" />
  <cs:Button Command="Search">Apply Filter</cs:Button>
</cs:Form>

<cs:Region Name="collection-table" Tag="section" class="book-table-card">
  <cs:Table Items="{Binding Books}" Columns="Id:Id,Title:Title,Status:Status" />
</cs:Region>
```

`cs:Region` renders as a normal element with `data-cs-region`. POST command responses include matching HTML patches so the browser can replace metrics, notices, or tables without reloading the full page.

### Scheduled Tasks

```csharp
[Service]
public class Jobs
{
    [Scheduled("0 */5 * * * ?")]   // cron
    public async Task SyncDataAsync() { }

    [Every(60)]                     // every 60 seconds
    public async Task HealthCheckAsync() { }

    [OnStartup(2000)]              // 2s after boot
    public async Task WarmCacheAsync() { }
}
```

### Typed HTTP Client

```csharp
[Service]
public class ApiClient(IHttp http)
{
    public Task<List<User>> GetUsers() =>
        http.Get<List<User>>("https://api.example.com/users");
}
```

### AOP Interceptors

```csharp
[Service]
public class OrderService
{
    [Transactional]
    public async Task CreateAsync(Order order) { /* auto-commit/rollback */ }

    [Cacheable(CacheKey = "orders:{0}", ExpirationSeconds = 300)]
    public async Task<Order> GetByIdAsync(long id) { /* cached result */ }
}
```

### Module System

Conditional auto-configuration with dependency resolution:

```csharp
[Require(typeof(RedisCacheModule))]
public class CachingModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext ctx)
    {
        ctx.Services.AddRedisCache(ctx.Configuration);
    }
}
```

### Production-Ready Actuator

| Endpoint | Description |
|----------|-------------|
| `/actuator/health` | Liveness/readiness probes |
| `/actuator/metrics` | Application metrics |
| `/actuator/info` | Build and runtime info |

## Project Structure

```
src/
├── CodeSpirit.Core/              # Attributes, abstractions, ViewModel base
├── CodeSpirit.Infrastructure/   # Implementations
│   ├── AutoConfiguration/       # Service scanning + module loader
│   ├── Scheduling/              # Background jobs
│   ├── Http/                    # Typed HTTP client
│   ├── EntityFramework/         # Repository pattern
│   ├── Mvvm/                    # ViewModel executor
│   ├── Page/                    # ASPX parser + HTML renderer
│   ├── Aop/                     # Transaction/cache interceptors
│   ├── Authentication/          # JWT
│   ├── Caching / Redis/         # Cache + distributed cache
│   ├── Messaging/              # RabbitMQ event bus
│   ├── Monitoring/             # Actuator endpoints
│   ├── Telemetry/              # OpenTelemetry
│   └── Logging/               # Serilog
├── CodeSpirit.SourceGenerator/ # Compile-time service registration
├── CodeSpirit.Modules/        # Example business modules
├── CodeSpirit.Host/           # Demo host application
├── CodeSpirit.LibraryManagement/ # Enterprise library management sample app
├── CodeSpirit.Tests/          # Unit tests
└── Templates/                 # Project templates
    └── CodeSpiritVsixTemplate/  # Visual Studio VSIX template
```

## Web Template Layout

Both `dotnet new codespirit-library` and the VSIX template generate a complete project with organized folders:

```
$safeprojectname$/
├── Features/                    # Business modules grouped by capability
│   ├── Admin/                   # Admin MVVM page model
│   ├── Home/                    # Dashboard MVVM page model
│   ├── Library/                 # Library domain models and services
│   │   ├── Models/
│   │   └── Services/
│   └── Weather/                 # Example API + MVVM module
│       ├── Controllers/
│       ├── Models/
│       └── Services/
├── Pages/                       # .aspx pages + Site.master layout
├── Components/                  # Reusable .ascx UI components
├── Reports/                     # XML report templates (.rpt.xml)
├── wwwroot/                     # Static assets (CSS/JS/images/fonts)
│   ├── css/
│   └── js/
├── scripts/                     # Project-local validation and tooling scripts
├── Program.cs                   # [CodeSpiritApplication] entry point
└── appsettings.json             # Framework + app configuration
```

Business code uses feature folders so enterprise applications can grow by module. Framework convention folders stay at the project root because pages, components, static assets, and reports are discovered by path.

## Configuration

```json
{
  "CodeSpirit": {
    "Name": "MyApp",
    "Version": "1.0.0",
    "Profile": "development",
    "Actuator": {
      "EnableHealthEndpoint": true,
      "EnableMetricsEndpoint": true,
      "EnableInfoEndpoint": true
    }
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
dotnet test src/CodeSpirit.Tests
dotnet run --project src/CodeSpirit.LibraryManagement
```

## Developer Tooling

### Code Snippets

The VSIX template installs the following code snippets, available in any C# or HTML file. Type the shortcut and press Tab to expand:

| Shortcut | Language | Expands To |
|----------|----------|------------|
| `csvm` | C# | Complete ViewModel with PageDirective, Bind, Command, LoadAsync |
| `cssvc` | C# | Service class with [Service] and [Autowired] logger |
| `csapp` | C# | Program.cs CodeSpirit application entry point |
| `csmod` | C# | CodeSpiritModule auto-configuration class |
| `csbind` | C# | [Bind] property declaration |
| `csform` | ASPX | `<cs:Form>` with Field and Button |
| `cstable` | ASPX | `<cs:Table>` with Column definitions |
| `csregion` | ASPX | `<cs:Region>` partial update area |

### Compile-Time Diagnostics

The source generator reports the following rules in the Visual Studio Error List:

| ID | Severity | Description |
|----|----------|-------------|
| CSP001 | Warning | Abstract class with `[Service]` is ignored |
| CSP002 | Warning | `[Service]` class has no public constructor |
| CSP003 | Error | `[Command]` method must not declare parameters |

### EditorConfig

The repo includes `.editorconfig` with file-scoped namespace enforcement, consistent indentation (4-space C#, 2-space web), UTF-8/LF, and style preferences for `var` usage, pattern matching, and expression-bodied members.

## Requirements

- .NET 10 SDK
- Visual Studio 2022 17.10+ or VS2026 (for VSIX template, build the VSIX project)

## License

MIT
