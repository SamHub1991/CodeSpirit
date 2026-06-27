using CodeSpirit.Core.Attributes;
using CodeSpirit.Infrastructure.AutoConfiguration;
using CodeSpirit.Infrastructure.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

[CodeSpiritApplication]
public class Program
{
    private static readonly string[] DevSyncAttributes =
    [
        "class", "style", "data-ui", "data-cs-tone", "data-cs-intent", "data-cs-class",
        "data-cs-show", "data-cs-enable", "data-cs-refresh", "data-cs-confirm", "data-cs-source",
        "data-cs-attr", "data-cs-visible", "data-cs-hidden", "data-cs-enabled", "data-cs-disabled"
    ];

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseCodeSpiritSerilog(builder.Configuration);

        builder.AddCodeSpirit();

        var app = builder.Build();

        app.UseCodeSpirit();

        if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("CodeSpirit:EnableDevSync"))
        {
            app.MapPost("/dev/api/sync-config", async (HttpContext ctx) =>
            {
                if (!IsLocalHost(ctx.Request.Host.Host))
                {
                    await WriteJsonError(ctx, 403, "Dev sync is only available from localhost.");
                    return;
                }

                DevSyncRequest? body;
                try
                {
                    body = await JsonSerializer.DeserializeAsync<DevSyncRequest>(ctx.Request.Body);
                }
                catch (JsonException)
                {
                    await WriteJsonError(ctx, 400, "Invalid JSON payload.");
                    return;
                }

                if (body is null || string.IsNullOrWhiteSpace(body.Page) || string.IsNullOrWhiteSpace(body.Snippet))
                {
                    await WriteJsonError(ctx, 400, "Missing page or snippet.");
                    return;
                }

                var validationError = ValidateSyncRequest(body);
                if (validationError is not null)
                {
                    await WriteJsonError(ctx, 400, validationError);
                    return;
                }

                try
                {
                    var pagePath = ResolvePagePath(body.Page);
                    if (pagePath is null)
                    {
                        await WriteJsonError(ctx, 404, $"Page not found: {body.Page}.aspx");
                        return;
                    }

                    var content = await File.ReadAllTextAsync(pagePath);
                    var tag = NormalizeTag(body.ElementTag);
                    var match = FindSourceTag(content, tag, body.SourceTag);

                    if (!match.Success)
                    {
                        await WriteJsonError(ctx, 404, $"No matching <{tag}> element found in .aspx file.");
                        return;
                    }

                    var replacement = SyncAttributes(match.Value, body.Snippet);
                    content = content.Replace(match.Value, replacement);

                    await File.WriteAllTextAsync(pagePath, content);

                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { ok = true, page = Path.GetFileNameWithoutExtension(pagePath) }));
                }
                catch (Exception ex)
                {
                    await WriteJsonError(ctx, 500, ex.Message);
                }
            });
        }

        app.Run();
    }

    private static string SyncAttributes(string originalTag, string snippet)
    {
        var snippetAttrs = new Dictionary<string, string>();
        foreach (var attr in DevSyncAttributes)
        {
            var regex = new Regex($@"{attr}=""([^""]*)""", RegexOptions.IgnoreCase);
            var m = regex.Match(snippet);
            if (m.Success)
            {
                snippetAttrs[attr] = m.Groups[1].Value;
            }
        }

        foreach (var attr in DevSyncAttributes)
        {
            if (snippetAttrs.ContainsKey(attr))
                continue;

            var regex = new Regex($@"\s*{attr}=""[^""]*""", RegexOptions.IgnoreCase);
            originalTag = regex.Replace(originalTag, string.Empty);
        }

        foreach (var kvp in snippetAttrs)
        {
            var regex = new Regex($@"{kvp.Key}=""[^""]*""", RegexOptions.IgnoreCase);
            if (regex.IsMatch(originalTag))
            {
                originalTag = regex.Replace(originalTag, $"{kvp.Key}=\"{kvp.Value}\"");
            }
            else
            {
                var insertPos = originalTag.IndexOf('>');
                if (insertPos >= 0)
                {
                    originalTag = originalTag.Insert(insertPos, $" {kvp.Key}=\"{kvp.Value}\"");
                }
            }
        }

        foreach (var attr in snippetAttrs.Keys)
        {
            if (string.IsNullOrWhiteSpace(snippetAttrs[attr]))
            {
                var regex = new Regex($@"\s*{attr}=""[^""]*""", RegexOptions.IgnoreCase);
                originalTag = regex.Replace(originalTag, string.Empty);
            }
        }

        return originalTag;
    }

    private static string? ValidateSyncRequest(DevSyncRequest body)
    {
        if (body.Page.Length > 80 || !Regex.IsMatch(body.Page, @"^[A-Za-z0-9_-]+$"))
            return "Invalid page name.";

        if (body.Snippet.Length > 20_000)
            return "Snippet is too large.";

        if (!string.IsNullOrWhiteSpace(body.SourceTag) && body.SourceTag.Length > 4_000)
            return "Source tag is too large.";

        var tag = NormalizeTag(body.ElementTag);
        if (!Regex.IsMatch(tag, @"^[a-z][a-z0-9-]*$"))
            return "Invalid element tag.";

        return null;
    }

    private static string NormalizeTag(string? tag)
    {
        var value = (tag ?? string.Empty).Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(value) ? "div" : value;
    }

    private static Task WriteJsonError(HttpContext ctx, int statusCode, string message)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new { ok = false, error = message }));
    }

    private static bool IsLocalHost(string? host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);
    }

    private static Match FindSourceTag(string content, string tag, string? sourceTag)
    {
        if (!string.IsNullOrWhiteSpace(sourceTag))
        {
            var exact = Regex.Match(content, Regex.Escape(sourceTag), RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (exact.Success)
                return exact;
        }

        var searchPattern = $@"<\s*{Regex.Escape(tag)}\b[^>]*\b(data-ui|data-cs-(tone|intent|class|bind|show|enable|refresh|confirm|source|attr|visible|hidden|enabled|disabled))\b[^>]*>";
        var match = Regex.Match(content, searchPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
            return match;

        searchPattern = $@"<\s*{Regex.Escape(tag)}\b[^>]*>";
        return Regex.Match(content, searchPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    private static string? ResolvePagePath(string pageName)
    {
        var normalized = (pageName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        normalized = normalized.Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace("\\", string.Empty, StringComparison.Ordinal);
        normalized = Path.GetFileNameWithoutExtension(normalized);

        var pagesDir = ResolvePagesDirectory();
        if (pagesDir is null)
            return null;

        foreach (var candidate in Directory.EnumerateFiles(pagesDir, "*.aspx", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(Path.GetFileNameWithoutExtension(candidate), normalized, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        var direct = Path.Combine(pagesDir, $"{normalized}.aspx");
        return File.Exists(direct) ? direct : null;
    }

    private static string? ResolvePagesDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var projectPages = Path.Combine(current.FullName, "Pages");
            var projectFile = Path.Combine(current.FullName, "CodeSpirit.LibraryManagement.csproj");
            if (Directory.Exists(projectPages) && File.Exists(projectFile))
                return projectPages;

            var sourcePages = Path.Combine(current.FullName, "src", "CodeSpirit.LibraryManagement", "Pages");
            if (Directory.Exists(sourcePages))
                return sourcePages;

            current = current.Parent;
        }

        var outputPages = Path.Combine(AppContext.BaseDirectory, "Pages");
        return Directory.Exists(outputPages) ? outputPages : null;
    }

    private record DevSyncRequest(string Page, string Snippet, string? ElementTag, string? SourceTag);
}
