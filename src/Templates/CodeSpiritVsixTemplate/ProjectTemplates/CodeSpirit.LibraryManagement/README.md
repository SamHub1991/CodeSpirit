# CodeSpirit Library Management

Enterprise library management sample application built with CodeSpirit.

## Directory Layout

```text
CodeSpirit.LibraryManagement/
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
├── Pages/                       # ASPX pages and Site.master layout
├── Components/                  # Reusable ASCX components
├── Reports/                     # XML report templates
├── wwwroot/                     # Static CSS, JavaScript, images, and fonts
├── Program.cs                   # CodeSpirit application entry point
└── appsettings.json             # Application configuration
```

## Module Guidelines

- Put business code under `Features/{ModuleName}`.
- Put page ViewModels beside their module, for example `Features/Admin/AdminViewModel.cs`.
- Put shared domain services and models under a capability folder, for example `Features/Library`.
- Keep framework convention files in root folders: `Pages`, `Components`, `Reports`, and `wwwroot`.
- Keep ASPX files focused on markup; `Route` and `Title` live on `PageDirective`, and the default layout is `Pages/Site.master`.

## CSV Import and Export

The admin page supports catalog import and export through the existing MVVM command flow. Use the CSV workspace on `/admin` to export the current catalog filter or import edited rows.

Expected CSV columns:

```text
ISBN,Title,Author,Category,Location,PublishedYear,CopyCount,Rating
```

- Existing ISBN values update catalog records.
- New ISBN values add catalog records.
- Rows without title or author are skipped and reported in the notice area.

## Built-in Page Tags

- `cs:Content` fills layout placeholders such as `Head`, `Body`, and `Scripts`.
- `cs:PlaceHolder` marks layout regions in `Pages/Site.master`.
- `cs:Repeater` renders a collection with item-level bindings.
- `cs:Conditional` renders content when the bound value is truthy.
- `cs:Link` renders an encoded anchor with binding support in `NavigateTo`.
- `cs:Form` renders a standard MVVM form with `method="post"` and `data-cs-vm`.
- `cs:Button` renders a submit button with `data-cs-command`.
- `cs:Field` renders a bound input or textarea from `Name`, `Label`, `Placeholder`, `Type`, and `Rows`.
- `cs:Table` renders a table from `Items`, `Columns`, and optional `EmptyText`.
- `cs:Table` also supports nested `cs:Column` blocks for custom cell templates.
- `cs:Region` renders a named `data-cs-region` element for command response HTML patches.

## Developer Hints

### Code Snippets

After installing this VSIX, the following shortcuts are available in any C# or ASPX file:

| Shortcut | Output |
|----------|--------|
| `csvm` | ViewModel with PageDirective, Bind, Command, LoadAsync |
| `cssvc` | Service class with [Service] and [Autowired] |
| `csapp` | Program.cs entry point |
| `csmod` | CodeSpiritModule template |
| `csbind` | [Bind] property |
| `csform` | `<cs:Form>` with Field and Button |
| `cstable` | `<cs:Table>` with Columns |
| `csregion` | `<cs:Region>` partial update block |

### Compile-Time Diagnostics

The source generator reports these in the Error List:

- **CSP001** (warning): Abstract `[Service]` class is not registered.
- **CSP002** (warning): `[Service]` class has no public constructor.
- **CSP003** (error): `[Command]` method declares parameters.

## Validation

```bash
dotnet build CodeSpirit.LibraryManagement.csproj
```
