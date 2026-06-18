namespace CodeSpirit.Core.Page;

/// <summary>
/// Configures a ViewModel class as a routable page.
/// Controls the URL route, layout, and page title.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [PageDirective(Route = "/admin", Title = "Library Admin")]
/// [PageDirective(Route = "/weather", Title = "Forecast", Layout = "Pages/Sidebar.master")]
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class PageDirectiveAttribute : Attribute
{
    /// <summary>
    /// URL route for the page (e.g. "/admin", "/weather").
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// Optional ViewModel type when the page is not the ViewModel itself.
    /// </summary>
    public string? ViewModelType { get; set; }

    /// <summary>
    /// Layout master page path. Defaults to <c>Pages/Site.master</c>.
    /// </summary>
    public string? Layout { get; set; }

    /// <summary>
    /// Browser title for the page.
    /// </summary>
    public string? Title { get; set; }
}