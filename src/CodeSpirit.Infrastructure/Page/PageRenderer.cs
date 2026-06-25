using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSpirit.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Infrastructure.Page;

public class PageRenderer
{
    private const string DefaultLayout = "Pages/Site.master";

    private readonly PageParser _parser;
    private readonly ILogger<PageRenderer> _logger;
    private static readonly RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;
    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> _propCache = new();

    private static Regex TagBlock(string tag) => new(
        $@"<cs:{tag}(?<attrs>[^>]*)>(?<content>.*?)</cs:{tag}>", DefaultOptions);

    private static Regex SelfClosing(string tag) => new(
        $@"<cs:{tag}(?<attrs>[^>]*)\s*/>", DefaultOptions);

    private static readonly Regex ContentRegex = new(
        @"<cs:Content\s+PlaceHolder=\""(?<name>[^\"" ]+)\""\s*>(?<content>.*?)</cs:Content>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex RepeaterRegex = new(
        @"<cs:Repeater\s+Items=\""\{Binding\s+(?<name>[^}:]+)\}\""\s*>(?<content>.*?)</cs:Repeater>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex ConditionalRegex = new(
        @"<cs:Conditional\s+Visible=\""\{Binding\s+(?<name>[^}:]+)\}\""\s*>(?<content>.*?)</cs:Conditional>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex LinkRegex = new(
        @"<cs:Link\s+NavigateTo=\""(?<href>[^\"" ]+)\""\s*>(?<content>.*?)</cs:Link>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex FormRegex = TagBlock("Form");
    private static readonly Regex RegionRegex = TagBlock("Region");
    private static readonly Regex ButtonRegex = new(
        @"<cs:Button\s+Command=\""(?<command>[^\"" ]+)\""(?<attrs>[^>]*)>(?<content>.*?)</cs:Button>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex ShowRegex = new(
        @"<cs:Show(?<attrs>[^>]*)\s*>(?<content>.*?)</cs:Show>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex FieldRegex = SelfClosing("Field");
    private static readonly Regex TableRegex = SelfClosing("Table");
    private static readonly Regex TableBlockRegex = TagBlock("Table");
    private static readonly Regex ColumnRegex = TagBlock("Column");
    private static readonly Regex AttributeRegex = new(
        @"(?<name>[A-Za-z][A-Za-z0-9_-]*)=\""(?<value>[^\""]*)\""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BindingRegex = new(
        @"\{Binding\s+(?<name>[^}:]+)(:(?<format>[^}]+))?\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DirectiveRegex = new(
        @"<%@\s*(Page|Master)\b.*?%>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public PageRenderer(PageParser parser, ILogger<PageRenderer> logger)
    {
        _parser = parser;
        _logger = logger;
    }

    public async Task RenderAsync(HttpContext httpContext, Type viewModelType, Dictionary<string, object?> viewModelState)
    {
        var descriptor = _parser.ParseFromViewModelType(viewModelType);
        var title = descriptor?.Title ?? viewModelType.Name;
        viewModelState["PageTitle"] = title;

        var html = BuildHtml(httpContext, viewModelType, descriptor?.Layout, title, viewModelState);
        httpContext.Response.ContentType = "text/html; charset=utf-8";
        await httpContext.Response.WriteAsync(html);
    }

    public Dictionary<string, string> RenderRegions(HttpContext httpContext, Type viewModelType, Dictionary<string, object?> viewModelState)
    {
        var pagePath = ResolvePagePath(httpContext, viewModelType);
        if (pagePath is null)
            return [];

        var page = ReadPageContent(pagePath);
        var regions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in RegionRegex.Matches(page))
        {
            var name = GetAttribute(match.Groups["attrs"].Value, "Name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            regions[name] = RenderTemplate(match.Value, viewModelState);
        }

        return regions;
    }

    private static string BuildHtml(HttpContext httpContext, Type viewModelType, string? layout, string title, Dictionary<string, object?> state)
    {
        var pagePath = ResolvePagePath(httpContext, viewModelType);
        if (pagePath is null)
            return BuildFallbackHtml(title, state);

        var page = ReadPageContent(pagePath);
        var sections = ContentRegex.Matches(page)
            .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["content"].Value, StringComparer.OrdinalIgnoreCase);

        var body = sections.GetValueOrDefault("Body", page);
        var head = sections.GetValueOrDefault("Head", string.Empty);
        var scripts = sections.GetValueOrDefault("Scripts", string.Empty);

        var html = ResolveLayout(httpContext, layout) is { } layoutPath
            ? ReadPageContent(layoutPath)
                .Replace("<cs:PlaceHolder Name=\"Head\" />", head)
                .Replace("<cs:PlaceHolder Name=\"Body\" />", body)
                .Replace("<cs:PlaceHolder Name=\"Scripts\" />", scripts)
            : body;

        return RenderTemplate(html, state);
    }

    private static string BuildFallbackHtml(string title, Dictionary<string, object?> state)
    {
        var json = JsonSerializer.Serialize(state, CodeSpiritDefaults.JsonOptions);
        var propsHtml = BuildPropsHtml(state);
        var safeTitle = Html(title);

        return $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{safeTitle}}</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 2rem; background: #f5f5f5; }
        .cs-vm-data { background: #fff; border-radius: 8px; padding: 1.5rem; box-shadow: 0 1px 3px rgba(0,0,0,0.1); max-width: 800px; margin: 0 auto; }
        .cs-prop { border-bottom: 1px solid #eee; padding: 0.5rem 0; }
        .cs-prop:last-child { border-bottom: none; }
        .cs-key { font-weight: 600; color: #333; }
        .cs-val { color: #666; margin-left: 0.5rem; }
    </style>
</head>
<body>
    <div class="cs-vm-data" id="cs-app">
        <h1>{{safeTitle}}</h1>
        {{propsHtml}}
    </div>
    <script>
        window.__CS_VM_STATE__ = {{json}};
    </script>
</body>
</html>
""";
    }

    private static string BuildPropsHtml(Dictionary<string, object?> state)
    {
        if (state.Count == 0)
            return "<p><em>No bindable properties.</em></p>";

        var sb = new StringBuilder();
        foreach (var kvp in state)
        {
            sb.AppendLine($"        <div class=\"cs-prop\"><span class=\"cs-key\">{Html(kvp.Key)}:</span><span class=\"cs-val\">{Html(kvp.Value?.ToString() ?? "null")}</span></div>");
        }
        return sb.ToString();
    }

    private static string RenderTemplate(string template, IReadOnlyDictionary<string, object?> state)
    {
        var html = ConditionalRegex.Replace(template, match =>
            IsTruthy(GetValue(state, match.Groups["name"].Value)) ? match.Groups["content"].Value : string.Empty);

        html = RepeaterRegex.Replace(html, match =>
        {
            var value = GetValue(state, match.Groups["name"].Value);
            if (value is not System.Collections.IEnumerable items || value is string)
                return string.Empty;

            var itemTemplate = match.Groups["content"].Value;
            var sb = new StringBuilder();
            foreach (var item in items)
            {
                sb.Append(RenderBindings(itemTemplate, item));
            }
            return sb.ToString();
        });

        html = LinkRegex.Replace(html, match =>
        {
            var href = SafeUrl(RenderBindings(match.Groups["href"].Value, state));
            return $"<a href=\"{Html(href)}\">{match.Groups["content"].Value}</a>";
        });

        html = ButtonRegex.Replace(html, match =>
        {
            var command = Html(RenderBindings(match.Groups["command"].Value, state));
            var rawAttrs = match.Groups["attrs"].Value;
            var attrs = RenderHtmlAttributes(rawAttrs, state, "Command");

            if (string.IsNullOrWhiteSpace(GetAttribute(rawAttrs, "class")))
            {
                var btnClass = InferButtonClass(match.Groups["command"].Value);
                attrs = $" class=\"{btnClass}\"{attrs}";
            }

            return $"<button type=\"submit\" data-cs-command=\"{command}\"{attrs}>{match.Groups["content"].Value}</button>";
        });

        html = FieldRegex.Replace(html, match => RenderField(match.Groups["attrs"].Value, state));

        html = TableBlockRegex.Replace(html, match => RenderTable(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = TableRegex.Replace(html, match => RenderTable(match.Groups["attrs"].Value, string.Empty, state));

        html = ShowRegex.Replace(html, match => RenderShow(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = FormRegex.Replace(html, match =>
        {
            var attrs = RenderHtmlAttributes(match.Groups["attrs"].Value, state, "Method");
            var method = SafeFormMethod(GetAttribute(match.Groups["attrs"].Value, "Method"));
            return $"<form method=\"{Html(method)}\" data-cs-vm{attrs}>{match.Groups["content"].Value}</form>";
        });

        html = RegionRegex.Replace(html, match => RenderRegion(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        return DirectiveRegex.Replace(RenderBindings(html, state), string.Empty);
    }

    private static string RenderRegion(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var name = GetAttribute(rawAttributes, "Name");
        if (string.IsNullOrWhiteSpace(name))
            return RenderBindings(content, state);

        var tag = SafeTagName(GetAttribute(rawAttributes, "Tag"));
        var attrs = RenderHtmlAttributes(rawAttributes, state, "Name", "Tag");
        return $"<{tag} data-cs-region=\"{Html(name)}\"{attrs}>{content}</{tag}>";
    }

    private static string RenderShow(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var visibleBinding = GetAttribute(rawAttributes, "Visible");
        var hideBinding = GetAttribute(rawAttributes, "HideWhen");
        var classBinding = GetAttribute(rawAttributes, "Class");
        var className = GetAttribute(rawAttributes, "ClassName");
        var tag = SafeTagName(GetAttribute(rawAttributes, "Tag"));
        var attrs = RenderHtmlAttributes(rawAttributes, state, "Visible", "HideWhen", "Class", "ClassName", "Tag");
        var dataAttrs = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(visibleBinding))
        {
            var name = ExtractBindingName(visibleBinding);
            if (name is not null)
                dataAttrs.Append($" data-cs-visible=\"{Html(name)}\"");
        }

        if (!string.IsNullOrWhiteSpace(hideBinding))
        {
            var name = ExtractBindingName(hideBinding);
            if (name is not null)
                dataAttrs.Append($" data-cs-hidden=\"{Html(name)}\"");
        }

        if (!string.IsNullOrWhiteSpace(classBinding))
        {
            var name = ExtractBindingName(classBinding);
            if (name is not null)
            {
                var classExpr = Html(name);
                if (!string.IsNullOrWhiteSpace(className))
                    classExpr += ":" + Html(className);
                dataAttrs.Append($" data-cs-class=\"{classExpr}\"");
            }
        }

        return $"<{tag}{dataAttrs}{attrs}>{content}</{tag}>";
    }

    private static string RenderHtmlAttributes(string rawAttributes, IReadOnlyDictionary<string, object?> state, params string[] excluded)
    {
        var attributes = new StringBuilder();
        foreach (Match match in AttributeRegex.Matches(rawAttributes))
        {
            var name = match.Groups["name"].Value;
            if (excluded.Contains(name, StringComparer.OrdinalIgnoreCase))
                continue;

            if (!IsSafeHtmlAttributeName(name))
                continue;

            var value = RenderBindings(match.Groups["value"].Value, state);
            if (IsUrlAttribute(name))
                value = SafeUrl(value);

            attributes.Append(' ').Append(name).Append("=\"").Append(Html(value)).Append('"');
        }

        return attributes.ToString();
    }

    private static string RenderField(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var name = GetAttribute(rawAttributes, "Name");
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var label = GetAttribute(rawAttributes, "Label") ?? name;
        var type = (GetAttribute(rawAttributes, "Type") ?? "text").Trim();
        var rows = GetAttribute(rawAttributes, "Rows");
        var id = GetAttribute(rawAttributes, "Id") ?? name;
        var placeholder = GetAttribute(rawAttributes, "Placeholder");
        var commonAttributes = RenderHtmlAttributes(rawAttributes, state, "Name", "Label", "Type", "Rows", "Id", "Placeholder");
        var inputValue = Html(FormatValue(GetValue(state, name), null));

        if (type.Equals("textarea", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(rows))
        {
            var rowsAttribute = string.IsNullOrWhiteSpace(rows) ? string.Empty : $" rows=\"{Html(RenderBindings(rows, state))}\"";
            var placeholderAttribute = string.IsNullOrWhiteSpace(placeholder) ? string.Empty : $" placeholder=\"{Html(RenderBindings(placeholder, state))}\"";
            return $"<label for=\"{Html(id)}\">{Html(label)}<textarea id=\"{Html(id)}\" name=\"{Html(name)}\" data-cs-bind=\"{Html(name)}\"{rowsAttribute}{placeholderAttribute}{commonAttributes}>{inputValue}</textarea></label>";
        }

        var placeholderInput = string.IsNullOrWhiteSpace(placeholder) ? string.Empty : $" placeholder=\"{Html(RenderBindings(placeholder, state))}\"";
        return $"<label for=\"{Html(id)}\">{Html(label)}<input id=\"{Html(id)}\" type=\"{Html(type)}\" name=\"{Html(name)}\" value=\"{inputValue}\" data-cs-bind=\"{Html(name)}\"{placeholderInput}{commonAttributes} /></label>";
    }

    private static string RenderTable(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var itemsBinding = GetAttribute(rawAttributes, "Items");
        var columns = ParseColumns(GetAttribute(rawAttributes, "Columns"), content);
        if (string.IsNullOrWhiteSpace(itemsBinding) || columns.Count == 0)
            return string.Empty;

        var itemsName = ExtractBindingName(itemsBinding);
        var value = itemsName is null ? null : GetValue(state, itemsName);
        if (value is not System.Collections.IEnumerable items || value is string)
            return string.Empty;

        var attrs = RenderHtmlAttributes(rawAttributes, state, "Items", "Columns", "EmptyText");
        var header = string.Concat(columns.Select(column => $"<th>{Html(column.Header)}</th>"));
        var body = new StringBuilder();
        var count = 0;

        foreach (var item in items)
        {
            count++;
            body.Append("<tr>");
            foreach (var column in columns)
            {
                var cell = column.Template is null
                    ? Html(FormatValue(GetValue(item, column.Name), column.Format))
                    : RenderBindings(column.Template, item);

                if (column.Template is null)
                {
                    var intent = InferColumnIntent(column.Name);
                    if (intent is not null)
                    {
                        cell = $"<span data-cs-intent=\"{intent}\">{cell}</span>";
                    }
                }

                body.Append("<td>").Append(cell).Append("</td>");
            }
            body.Append("</tr>");
        }

        if (count == 0)
        {
            var emptyText = GetAttribute(rawAttributes, "EmptyText") ?? "No records.";
            body.Append($"<tr><td colspan=\"{columns.Count}\">{Html(emptyText)}</td></tr>");
        }

        return $"<table{attrs}><thead><tr>{header}</tr></thead><tbody>{body}</tbody></table>";
    }

    private static List<TableColumn> ParseColumns(string? rawColumns, string content)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            return ColumnRegex.Matches(content)
                .Select(match =>
                {
                    var name = GetAttribute(match.Groups["attrs"].Value, "Name") ?? string.Empty;
                    var header = GetAttribute(match.Groups["attrs"].Value, "Header") ?? name;
                    var format = GetAttribute(match.Groups["attrs"].Value, "Format");
                    return new TableColumn(name, header, format, match.Groups["content"].Value);
                })
                .Where(column => !string.IsNullOrWhiteSpace(column.Header))
                .ToList();
        }

        if (string.IsNullOrWhiteSpace(rawColumns))
            return [];

        return rawColumns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', 3, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            .Select(parts => new TableColumn(parts[0], parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : parts[0], parts.Length > 2 ? parts[2] : null, null))
            .ToList();
    }

    private static string? ExtractBindingName(string binding)
    {
        var match = BindingRegex.Match(binding);
        return match.Success ? match.Groups["name"].Value : binding;
    }

    private static string? GetAttribute(string rawAttributes, string targetName)
    {
        foreach (Match match in AttributeRegex.Matches(rawAttributes))
        {
            if (match.Groups["name"].Value.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return match.Groups["value"].Value;
        }

        return null;
    }

    private static string RenderBindings(string template, object? source)
    {
        return BindingRegex.Replace(template, match =>
        {
            var value = GetValue(source, match.Groups["name"].Value);
            var format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
            return Html(FormatValue(value, format));
        });
    }

    private static object? GetValue(object? source, string name)
    {
        if (source is null)
            return null;

        if (source is IReadOnlyDictionary<string, object?> dictionary && dictionary.TryGetValue(name.Trim(), out var value))
            return value;

        var type = source.GetType();
        var propName = name.Trim();
        var prop = _propCache.GetOrAdd((type, propName), static key =>
            key.Type.GetProperty(key.Name));
        return prop?.GetValue(source);
    }

    private static string FormatValue(object? value, string? format)
    {
        if (value is null)
            return string.Empty;

        return !string.IsNullOrWhiteSpace(format) && value is IFormattable formattable
            ? formattable.ToString(format, null)
            : value.ToString() ?? string.Empty;
    }

    private static bool IsTruthy(object? value) => value switch
    {
        bool b => b,
        int i => i != 0,
        string s => !string.IsNullOrWhiteSpace(s),
        System.Collections.IEnumerable items when value is not string => items.Cast<object?>().Any(),
        null => false,
        _ => true
    };

    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);

    private static string SafeTagName(string? tag)
    {
        var value = (tag ?? string.Empty).Trim().ToLowerInvariant();
        return Regex.IsMatch(value, @"^[a-z][a-z0-9-]*$") ? value : "div";
    }

    private static bool IsSafeHtmlAttributeName(string name)
    {
        return !name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(name, "srcdoc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUrlAttribute(string name)
    {
        return string.Equals(name, "href", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "src", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "action", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "formaction", StringComparison.OrdinalIgnoreCase);
    }

    private static string SafeUrl(string? value)
    {
        var url = (value ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (url.StartsWith("/", StringComparison.Ordinal) || url.StartsWith("#", StringComparison.Ordinal))
            return url;

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto || uri.Scheme == "tel"))
            return url;

        return "#";
    }

    private static string SafeFormMethod(string? method)
    {
        return string.Equals(method, "get", StringComparison.OrdinalIgnoreCase) ? "get" : "post";
    }

    private static string InferButtonClass(string rawCommand)
    {
        var command = (rawCommand ?? string.Empty).ToLowerInvariant();

        if (command.Contains("delete") || command.Contains("archive") || command.Contains("remove") ||
            command.Contains("suspend") || command.Contains("writeoff") || command.Contains("cancel") ||
            command.Contains("reject"))
            return "cs-btn cs-btn-danger";

        if (command.Contains("add") || command.Contains("create") || command.Contains("register") ||
            command.Contains("import") || command.Contains("collect") || command.Contains("borrow") ||
            command.Contains("reserve") || command.Contains("save") || command.Contains("submit"))
            return "cs-btn";

        if (command.Contains("clear") || command.Contains("dismiss") || command.Contains("back"))
            return "cs-btn cs-btn-ghost";

        return "cs-btn cs-btn-secondary";
    }

    private static string? InferColumnIntent(string columnName)
    {
        var name = (columnName ?? string.Empty).ToLowerInvariant();

        if (name is "status" or "state" || name.Contains("level"))
            return "status";

        if (name.Contains("due") || name.Contains("date") || name.EndsWith("at") ||
            name.Contains("borrowed") || name.Contains("returned") || name.Contains("created"))
            return "due";

        if (name.Contains("count") || name.Contains("total") || name.EndsWith("qty") ||
            name.Contains("copies") || name.Contains("fine") || name.Contains("balance") ||
            name.Contains("rating") || name.Contains("renew") || name.Contains("loans") ||
            name.Contains("reservations") || name.Contains("amount"))
            return "numeric";

        return null;
    }

    private sealed record TableColumn(string Name, string Header, string? Format, string? Template);

    private static string? ResolvePagePath(HttpContext context, Type viewModelType)
    {
        var name = viewModelType.Name.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase)
            ? viewModelType.Name[..^"ViewModel".Length]
            : viewModelType.Name;

        var relativeName = $"Pages/{name}.aspx";
        return ResolveContent(relativeName);
    }

    private static string? ResolveLayout(HttpContext context, string? layout)
    {
        var configuredLayout = string.IsNullOrWhiteSpace(layout) ? DefaultLayout : layout;
        var relative = configuredLayout.StartsWith("~/", StringComparison.Ordinal) ? configuredLayout[2..] : configuredLayout;
        return ResolveContent(relative);
    }

    private static string? ResolveContent(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);

        var physicalPath = Path.Combine(AppContext.BaseDirectory, normalizedPath);
        if (File.Exists(physicalPath))
            return physicalPath;

        var embeddedName = normalizedPath.Replace(Path.DirectorySeparatorChar, '.');

        var assembly = Assembly.GetEntryAssembly();
        if (assembly?.GetManifestResourceStream($"{assembly.GetName().Name}.{embeddedName}") != null)
            return $"embedded:{assembly.GetName().Name}.{embeddedName}";

        var infraAssembly = typeof(PageRenderer).Assembly;
        if (infraAssembly.GetManifestResourceStream($"{infraAssembly.GetName().Name}.{embeddedName}") != null)
            return $"embedded:{infraAssembly.GetName().Name}.{embeddedName}";

        return null;
    }

    private static string ReadPageContent(string pathOrEmbedded)
    {
        if (pathOrEmbedded.StartsWith("embedded:", StringComparison.Ordinal))
        {
            var resourceName = pathOrEmbedded["embedded:".Length..];
            var assembly = Assembly.GetEntryAssembly();
            using var stream = assembly?.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }

            var infraAssembly = typeof(PageRenderer).Assembly;
            using var infraStream = infraAssembly.GetManifestResourceStream(resourceName);
            if (infraStream != null)
            {
                using var reader = new StreamReader(infraStream);
                return reader.ReadToEnd();
            }
        }

        return File.ReadAllText(pathOrEmbedded);
    }

    /// <summary>
    /// Exposes template rendering for tests.
    /// </summary>
    public static string RenderTemplateForTests(string template, IReadOnlyDictionary<string, object?> state)
        => RenderTemplate(template, state);
}
