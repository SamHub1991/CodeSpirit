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

## Validation

```bash
dotnet build CodeSpirit.LibraryManagement.csproj
```
