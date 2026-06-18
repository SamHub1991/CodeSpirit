using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSpirit.Core.Mvvm;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CodeSpirit.Infrastructure.Page;

public class PageRenderer
{
    private const string DefaultLayout = "Pages/Site.master";

    private readonly PageParser _parser;
    private readonly ILogger<PageRenderer> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
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
    private static readonly Regex FormRegex = new(
        @"<cs:Form(?<attrs>[^>]*)>(?<content>.*?)</cs:Form>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex ButtonRegex = new(
        @"<cs:Button\s+Command=\""(?<command>[^\"" ]+)\""(?<attrs>[^>]*)>(?<content>.*?)</cs:Button>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex FieldRegex = new(
        @"<cs:Field(?<attrs>[^>]*)\s*/>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
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

    private static string BuildHtml(HttpContext httpContext, Type viewModelType, string? layout, string title, Dictionary<string, object?> state)
    {
        var pagePath = ResolvePagePath(httpContext, viewModelType);
        if (pagePath is null)
            return BuildFallbackHtml(title, state);

        var page = File.ReadAllText(pagePath);
        var sections = ContentRegex.Matches(page)
            .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["content"].Value, StringComparer.OrdinalIgnoreCase);

        var body = sections.GetValueOrDefault("Body", page);
        var head = sections.GetValueOrDefault("Head", string.Empty);
        var scripts = sections.GetValueOrDefault("Scripts", string.Empty);

        var html = ResolveLayout(httpContext, layout) is { } layoutPath
            ? File.ReadAllText(layoutPath)
                .Replace("<cs:PlaceHolder Name=\"Head\" />", head)
                .Replace("<cs:PlaceHolder Name=\"Body\" />", body)
                .Replace("<cs:PlaceHolder Name=\"Scripts\" />", scripts)
            : body;

        return RenderTemplate(html, state);
    }

    private static string BuildFallbackHtml(string title, Dictionary<string, object?> state)
    {
        var json = JsonSerializer.Serialize(state, JsonOptions);
        var propsHtml = BuildPropsHtml(state);

        return $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>{{title}}</title>
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
        <h1>{{title}}</h1>
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
            sb.AppendLine($"        <div class=\"cs-prop\"><span class=\"cs-key\">{kvp.Key}:</span><span class=\"cs-val\">{System.Net.WebUtility.HtmlEncode(kvp.Value?.ToString() ?? "null")}</span></div>");
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
            var href = RenderBindings(match.Groups["href"].Value, state);
            return $"<a href=\"{Html(href)}\">{match.Groups["content"].Value}</a>";
        });

        html = ButtonRegex.Replace(html, match =>
        {
            var command = Html(RenderBindings(match.Groups["command"].Value, state));
            var attrs = RenderHtmlAttributes(match.Groups["attrs"].Value, state, "Command");
            return $"<button type=\"submit\" data-cs-command=\"{command}\"{attrs}>{match.Groups["content"].Value}</button>";
        });

        html = FieldRegex.Replace(html, match => RenderField(match.Groups["attrs"].Value, state));

        html = FormRegex.Replace(html, match =>
        {
            var attrs = RenderHtmlAttributes(match.Groups["attrs"].Value, state, "Method");
            var method = GetAttribute(match.Groups["attrs"].Value, "Method") ?? "post";
            return $"<form method=\"{Html(method)}\" data-cs-vm{attrs}>{match.Groups["content"].Value}</form>";
        });

        return DirectiveRegex.Replace(RenderBindings(html, state), string.Empty);
    }

    private static string RenderHtmlAttributes(string rawAttributes, IReadOnlyDictionary<string, object?> state, params string[] excluded)
    {
        var attributes = new StringBuilder();
        foreach (Match match in AttributeRegex.Matches(rawAttributes))
        {
            var name = match.Groups["name"].Value;
            if (excluded.Contains(name, StringComparer.OrdinalIgnoreCase))
                continue;

            var value = RenderBindings(match.Groups["value"].Value, state);
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

        var prop = source.GetType().GetProperty(name.Trim());
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

    private static string? ResolvePagePath(HttpContext context, Type viewModelType)
    {
        var name = viewModelType.Name.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase)
            ? viewModelType.Name[..^"ViewModel".Length]
            : viewModelType.Name;
        var path = Path.Combine(AppContext.BaseDirectory, "Pages", name + ".aspx");
        return File.Exists(path) ? path : null;
    }

    private static string? ResolveLayout(HttpContext context, string? layout)
    {
        var configuredLayout = string.IsNullOrWhiteSpace(layout) ? DefaultLayout : layout;
        var relative = configuredLayout.StartsWith("~/", StringComparison.Ordinal) ? configuredLayout[2..] : configuredLayout;
        var path = Path.Combine(AppContext.BaseDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path) ? path : null;
    }
}
