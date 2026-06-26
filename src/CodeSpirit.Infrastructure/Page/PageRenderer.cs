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
        $@"<cs:{tag}\b(?<attrs>[^>]*)>(?<content>.*?)</cs:{tag}>", DefaultOptions);

    private static Regex SelfClosing(string tag) => new(
        $@"<cs:{tag}\b(?<attrs>[^>]*)\s*/>", DefaultOptions);

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
    private static readonly Regex ScriptsBlockRegex = TagBlock("Scripts");
    private static readonly Regex ScriptsRegex = SelfClosing("Scripts");
    private static readonly Regex ScriptRegex = SelfClosing("Script");
    private static readonly Regex FieldRegex = SelfClosing("Field");
    private static readonly Regex TableRegex = SelfClosing("Table");
    private static readonly Regex TableBlockRegex = TagBlock("Table");
    private static readonly Regex ColumnRegex = TagBlock("Column");
    private static readonly Regex ToolbarRegex = TagBlock("Toolbar");
    private static readonly Regex TabsRegex = TagBlock("Tabs");
    private static readonly Regex TabRegex = TagBlock("Tab");
    private static readonly Regex ModalRegex = TagBlock("Modal");
    private static readonly Regex PagerRegex = SelfClosing("Pager");
    private static readonly Regex GridRegex = TagBlock("Grid");
    private static readonly Regex CardRegex = TagBlock("Card");
    private static readonly Regex StackRegex = TagBlock("Stack");
    private static readonly Regex CrudRegex = SelfClosing("Crud");
    private static readonly Regex DashboardRegex = TagBlock("Dashboard");
    private static readonly Regex MetricCardRegex = SelfClosing("MetricCard");
    private static readonly Regex ActivityFeedRegex = SelfClosing("ActivityFeed");
    private static readonly Regex QuickLinksRegex = SelfClosing("QuickLinks");
    private static readonly Regex ChartRegex = SelfClosing("Chart");
    private static readonly Regex TreeRegex = SelfClosing("Tree");
    private static readonly Regex WizardRegex = TagBlock("Wizard");
    private static readonly Regex StepRegex = TagBlock("Step");
    private static readonly Regex AttributeRegex = new(
        @"(?<name>[A-Za-z][A-Za-z0-9_-]*)=\""(?<value>[^\""]*)\""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BindingRegex = new(
        @"\{Binding\s+(?<name>[^}:]+)(:(?<format>[^}]+))?\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex DirectiveRegex = new(
        @"<%@\s*(Page|Master)\b.*?%>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly IReadOnlyList<ScriptAsset> BuiltInScripts =
    [
        new("Runtime", "/js/codespirit.runtime.js"),
        new("Expression", "/js/codespirit.expression.js"),
        new("JQueryLite", "/js/vendor/jquery-lite.js"),
        new("JQueryBehaviors", "/js/ui/jquery.behaviors.js"),
        new("UiBehaviors", "/js/ui/ui.behaviors.js"),
        new("Intent", "/js/ui/codespirit.intent.js"),
        new("DevPanel", "/js/ui/codespirit.devpanel.js"),
        new("Site", "/js/site.js")
    ];

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

        html = ToolbarRegex.Replace(html, match => RenderToolbar(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = TabsRegex.Replace(html, match => RenderTabs(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = ModalRegex.Replace(html, match => RenderModal(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = PagerRegex.Replace(html, match => RenderPager(match.Groups["attrs"].Value, state));

        html = ShowRegex.Replace(html, match => RenderShow(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = GridRegex.Replace(html, match => RenderGrid(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = CardRegex.Replace(html, match => RenderCard(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = StackRegex.Replace(html, match => RenderStack(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = CrudRegex.Replace(html, match => RenderCrud(match.Groups["attrs"].Value, state));

        html = MetricCardRegex.Replace(html, match => RenderMetricCards(match.Groups["attrs"].Value, state));

        html = ActivityFeedRegex.Replace(html, match => RenderActivityFeed(match.Groups["attrs"].Value, state));

        html = QuickLinksRegex.Replace(html, match => RenderQuickLinks(match.Groups["attrs"].Value, state));

        html = DashboardRegex.Replace(html, match => RenderDashboard(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = ChartRegex.Replace(html, match => RenderChart(match.Groups["attrs"].Value, state));

        html = TreeRegex.Replace(html, match => RenderTree(match.Groups["attrs"].Value, state));

        html = WizardRegex.Replace(html, match => RenderWizard(match.Groups["attrs"].Value, match.Groups["content"].Value, state));

        html = ScriptsBlockRegex.Replace(html, match => RenderScripts(match.Groups["attrs"].Value, state, match.Groups["content"].Value));

        html = ScriptsRegex.Replace(html, match => RenderScripts(match.Groups["attrs"].Value, state));

        html = ScriptRegex.Replace(html, match => RenderScript(match.Groups["attrs"].Value, state));

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

    private static string RenderGrid(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var columns = RenderBindings(GetAttribute(rawAttributes, "Columns") ?? string.Empty, state);
        var gap = RenderBindings(GetAttribute(rawAttributes, "Gap") ?? string.Empty, state);
        var tag = SafeTagName(GetAttribute(rawAttributes, "Tag"));
        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-grid" + BuildGridClass(columns), BuildGapStyle(gap), "Columns", "Gap", "BreakAt", "Tag");
        return $"<{tag}{attrs}>{content}</{tag}>";
    }

    private static string RenderCard(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var tag = SafeTagName(GetAttribute(rawAttributes, "Tag"));
        var tone = RenderBindings(GetAttribute(rawAttributes, "Tone") ?? string.Empty, state);
        var extraClass = string.IsNullOrWhiteSpace(tone) ? string.Empty : $" cs-card-{SafeClassToken(tone)}";
        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-card" + extraClass, string.Empty, "Tone", "Tag");
        return $"<{tag}{attrs}>{content}</{tag}>";
    }

    private static string RenderStack(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var direction = RenderBindings(GetAttribute(rawAttributes, "Direction") ?? string.Empty, state);
        var align = RenderBindings(GetAttribute(rawAttributes, "Align") ?? string.Empty, state);
        var gap = RenderBindings(GetAttribute(rawAttributes, "Gap") ?? string.Empty, state);
        var tag = SafeTagName(GetAttribute(rawAttributes, "Tag"));
        var extraClass = direction.Equals("Row", StringComparison.OrdinalIgnoreCase) ? " cs-row" : string.Empty;
        var style = string.Join(' ', new[] { BuildGapStyle(gap), BuildAlignStyle(align) }.Where(item => !string.IsNullOrWhiteSpace(item)));
        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-stack" + extraClass, style, "Direction", "Align", "Gap", "Tag");
        return $"<{tag}{attrs}>{content}</{tag}>";
    }

    private static string RenderCrud(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var entity = RenderBindings(GetAttribute(rawAttributes, "Entity") ?? "Record", state);
        var title = RenderBindings(GetAttribute(rawAttributes, "Title") ?? entity, state);
        var subtitle = RenderBindings(GetAttribute(rawAttributes, "Subtitle") ?? string.Empty, state);
        var method = SafeFormMethod(GetAttribute(rawAttributes, "Method"));
        var fields = ParseCrudFields(GetAttribute(rawAttributes, "Fields"));
        var commands = ParseCrudCommands(GetAttribute(rawAttributes, "Commands"));
        if (fields.Count == 0 && commands.Count == 0)
            return string.Empty;

        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-card cs-crud", string.Empty, "Entity", "Title", "Subtitle", "Fields", "Commands", "Method");
        var sb = new StringBuilder();
        sb.Append("<section").Append(attrs).Append('>');

        if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(subtitle))
        {
            sb.Append("<header class=\"cs-crud-header\">");
            if (!string.IsNullOrWhiteSpace(title))
                sb.Append("<h2>").Append(Html(title)).Append("</h2>");
            if (!string.IsNullOrWhiteSpace(subtitle))
                sb.Append("<p>").Append(Html(subtitle)).Append("</p>");
            sb.Append("</header>");
        }

        sb.Append("<form method=\"").Append(Html(method)).Append("\" data-cs-vm class=\"cs-stack\">");
        foreach (var field in fields)
        {
            sb.Append(RenderCrudField(field, state));
        }

        if (commands.Count > 0)
        {
            sb.Append("<div class=\"cs-row cs-wrap\">");
            foreach (var command in commands)
            {
                var confirm = string.IsNullOrWhiteSpace(command.Confirm) ? string.Empty : $" data-cs-confirm=\"{Html(command.Confirm)}\"";
                sb.Append("<button type=\"submit\" data-cs-command=\"")
                    .Append(Html(command.Name))
                    .Append("\" class=\"")
                    .Append(Html(InferButtonClass(command.Name)))
                    .Append('"')
                    .Append(confirm)
                    .Append('>')
                    .Append(Html(command.Text))
                    .Append("</button>");
            }
            sb.Append("</div>");
        }

        sb.Append("</form></section>");
        return sb.ToString();
    }

    private static string RenderDashboard(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var title = RenderBindings(GetAttribute(rawAttributes, "Title") ?? string.Empty, state);
        var subtitle = RenderBindings(GetAttribute(rawAttributes, "Subtitle") ?? string.Empty, state);
        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-dashboard", string.Empty, "Title", "Subtitle", "Tag");
        var tag = SafeTagName(GetAttribute(rawAttributes, "Tag"));
        var sb = new StringBuilder();
        sb.Append('<').Append(tag).Append(attrs).Append('>');

        if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(subtitle))
        {
            sb.Append("<header class=\"cs-dashboard-hero\">");
            if (!string.IsNullOrWhiteSpace(title))
                sb.Append("<h1>").Append(Html(title)).Append("</h1>");
            if (!string.IsNullOrWhiteSpace(subtitle))
                sb.Append("<p>").Append(Html(subtitle)).Append("</p>");
            sb.Append("</header>");
        }

        sb.Append(content).Append("</").Append(tag).Append('>');
        return sb.ToString();
    }

    private static string RenderMetricCards(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var items = GetEnumerableFromBinding(rawAttributes, "Items", state);
        if (items is null)
            return string.Empty;

        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-grid cs-grid-3 cs-metrics", string.Empty, "Items");
        var sb = new StringBuilder();
        sb.Append("<section").Append(attrs).Append('>');
        foreach (var item in items)
        {
            var label = FormatValue(GetValue(item, "Label"), null);
            var value = FormatValue(GetValue(item, "Value"), null);
            var hint = FormatValue(GetValue(item, "Hint"), null);
            var tone = FormatValue(GetValue(item, "Tone"), null);
            var toneClass = string.IsNullOrWhiteSpace(tone) ? string.Empty : " cs-metric-" + SafeClassToken(tone);
            sb.Append("<article class=\"cs-card cs-metric").Append(toneClass).Append("\">")
                .Append("<span>").Append(Html(label)).Append("</span>")
                .Append("<strong data-cs-intent=\"numeric\">").Append(Html(value)).Append("</strong>")
                .Append("<small>").Append(Html(hint)).Append("</small>")
                .Append("</article>");
        }
        sb.Append("</section>");
        return sb.ToString();
    }

    private static string RenderActivityFeed(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var items = GetEnumerableFromBinding(rawAttributes, "Items", state);
        if (items is null)
            return string.Empty;

        var title = RenderBindings(GetAttribute(rawAttributes, "Title") ?? "Activity", state);
        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-card cs-activity-feed", string.Empty, "Items", "Title");
        var sb = new StringBuilder();
        sb.Append("<section").Append(attrs).Append('>');
        if (!string.IsNullOrWhiteSpace(title))
            sb.Append("<h2>").Append(Html(title)).Append("</h2>");

        foreach (var item in items)
        {
            var time = FormatValue(GetValue(item, "Time"), null);
            var text = FormatValue(GetValue(item, "Text"), null);
            var tone = FormatValue(GetValue(item, "Tone"), null);
            var toneClass = string.IsNullOrWhiteSpace(tone) ? string.Empty : " cs-activity-" + SafeClassToken(tone);
            sb.Append("<div class=\"cs-activity-row").Append(toneClass).Append("\">")
                .Append("<time>").Append(Html(time)).Append("</time>")
                .Append("<span>").Append(Html(text)).Append("</span>")
                .Append("</div>");
        }
        sb.Append("</section>");
        return sb.ToString();
    }

    private static string RenderQuickLinks(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var items = GetEnumerableFromBinding(rawAttributes, "Items", state);
        if (items is null)
            return string.Empty;

        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-grid cs-grid-3 cs-quick-links", string.Empty, "Items");
        var sb = new StringBuilder();
        sb.Append("<section").Append(attrs).Append('>');
        foreach (var item in items)
        {
            var title = FormatValue(GetValue(item, "Title"), null);
            var description = FormatValue(GetValue(item, "Description"), null);
            var url = SafeUrl(FormatValue(GetValue(item, "Url"), null));
            sb.Append("<a class=\"cs-card cs-quick-link\" href=\"").Append(Html(url)).Append("\">")
                .Append("<h3>").Append(Html(title)).Append("</h3>")
                .Append("<p>").Append(Html(description)).Append("</p>")
                .Append("</a>");
        }
        sb.Append("</section>");
        return sb.ToString();
    }

    private static string RenderChart(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var type = RenderBindings(GetAttribute(rawAttributes, "Type") ?? "bar", state);
        var width = RenderBindings(GetAttribute(rawAttributes, "Width") ?? "400", state);
        var height = RenderBindings(GetAttribute(rawAttributes, "Height") ?? "300", state);
        var dataBinding = GetAttribute(rawAttributes, "Data");
        var labelsBinding = GetAttribute(rawAttributes, "Labels");
        var title = RenderBindings(GetAttribute(rawAttributes, "Title") ?? string.Empty, state);

        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-card cs-chart", string.Empty, "Type", "Width", "Height", "Data", "Labels", "Title");
        var sb = new StringBuilder();
        sb.Append("<section").Append(attrs).Append('>');

        if (!string.IsNullOrWhiteSpace(title))
            sb.Append("<h3>").Append(Html(title)).Append("</h3>");

        sb.Append("<canvas data-cs-chart=\"").Append(Html(type)).Append("\"");
        sb.Append(" data-cs-chart-width=\"").Append(Html(width)).Append("\"");
        sb.Append(" data-cs-chart-height=\"").Append(Html(height)).Append("\"");

        if (!string.IsNullOrWhiteSpace(dataBinding))
            sb.Append(" data-cs-chart-data=\"{Binding ").Append(Html(dataBinding)).Append("}\"");

        if (!string.IsNullOrWhiteSpace(labelsBinding))
            sb.Append(" data-cs-chart-labels=\"{Binding ").Append(Html(labelsBinding)).Append("}\"");

        sb.Append("></canvas>");
        sb.Append("</section>");
        return sb.ToString();
    }

    private static string RenderTree(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var items = GetEnumerableFromBinding(rawAttributes, "Items", state);
        if (items is null)
            return string.Empty;

        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-card cs-tree", string.Empty, "Items", "ChildrenField", "LabelField", "ValueField");
        var childrenField = RenderBindings(GetAttribute(rawAttributes, "ChildrenField") ?? "Children", state);
        var labelField = RenderBindings(GetAttribute(rawAttributes, "LabelField") ?? "Label", state);
        var valueField = RenderBindings(GetAttribute(rawAttributes, "ValueField") ?? "Value", state);

        var sb = new StringBuilder();
        sb.Append("<section").Append(attrs).Append('>');
        sb.Append("<ul class=\"cs-tree-root\" data-cs-tree");
        sb.Append(" data-cs-tree-children=\"").Append(Html(childrenField)).Append("\"");
        sb.Append(" data-cs-tree-label=\"").Append(Html(labelField)).Append("\"");
        sb.Append(" data-cs-tree-value=\"").Append(Html(valueField)).Append("\"");
        sb.Append(">");

        foreach (var item in items)
        {
            RenderTreeNode(sb, item, childrenField, labelField, valueField, state);
        }

        sb.Append("</ul></section>");
        return sb.ToString();
    }

    private static void RenderTreeNode(StringBuilder sb, object item, string childrenField, string labelField, string valueField, IReadOnlyDictionary<string, object?> state)
    {
        var label = FormatValue(GetValue(item, labelField), null);
        var value = FormatValue(GetValue(item, valueField), null);
        var children = GetValue(item, childrenField) as System.Collections.IEnumerable;
        var hasChildren = children != null && !(children is string);

        sb.Append("<li class=\"cs-tree-node");
        if (hasChildren) sb.Append(" cs-tree-node-has-children");
        sb.Append("\" data-cs-tree-value=\"").Append(Html(value)).Append("\">");
        sb.Append("<span class=\"cs-tree-label\">").Append(Html(label)).Append("</span>");

        if (hasChildren)
        {
            sb.Append("<ul class=\"cs-tree-children\">");
            foreach (var child in children)
            {
                RenderTreeNode(sb, child, childrenField, labelField, valueField, state);
            }
            sb.Append("</ul>");
        }

        sb.Append("</li>");
    }

    private static string RenderWizard(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var activeStep = RenderBindings(GetAttribute(rawAttributes, "ActiveStep") ?? "0", state);
        var attrs = RenderLayoutAttributes(rawAttributes, state, "cs-wizard", string.Empty, "ActiveStep");
        var steps = StepRegex.Matches(content)
            .Select((match, index) => new
            {
                Key = RenderBindings(GetAttribute(match.Groups["attrs"].Value, "Key") ?? $"step-{index + 1}", state),
                Title = RenderBindings(GetAttribute(match.Groups["attrs"].Value, "Title") ?? $"Step {index + 1}", state),
                Content = match.Groups["content"].Value,
                Index = index
            })
            .ToList();

        var sb = new StringBuilder();
        sb.Append("<div class=\"cs-wizard\" data-cs-wizard").Append(attrs).Append('>');

        sb.Append("<div class=\"cs-wizard-steps\">");
        foreach (var step in steps)
        {
            var isActive = string.Equals(activeStep, step.Index.ToString(), StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(activeStep, step.Key, StringComparison.OrdinalIgnoreCase);
            sb.Append("<div class=\"cs-wizard-step").Append(isActive ? " active" : "").Append("\" data-cs-wizard-step=\"").Append(Html(step.Key)).Append("\">");
            sb.Append("<span class=\"cs-wizard-step-number\">").Append(step.Index + 1).Append("</span>");
            sb.Append("<span class=\"cs-wizard-step-title\">").Append(Html(step.Title)).Append("</span>");
            sb.Append("</div>");
        }
        sb.Append("</div>");

        sb.Append("<div class=\"cs-wizard-content\">");
        foreach (var step in steps)
        {
            var isVisible = string.Equals(activeStep, step.Index.ToString(), StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(activeStep, step.Key, StringComparison.OrdinalIgnoreCase);
            sb.Append("<div class=\"cs-wizard-panel").Append(isVisible ? " active" : "").Append("\" data-cs-wizard-panel=\"").Append(Html(step.Key)).Append("\"");
            if (!isVisible) sb.Append(" style=\"display:none\"");
            sb.Append(">");
            sb.Append(RenderTemplate(step.Content, state));
            sb.Append("</div>");
        }
        sb.Append("</div>");

        sb.Append("</div>");
        return sb.ToString();
    }

    private static string RenderScripts(string rawAttributes, IReadOnlyDictionary<string, object?> state, string? content = null)
    {
        var attrs = AttributeRegex.Matches(rawAttributes)
            .ToDictionary(match => match.Groups["name"].Value, match => RenderBindings(match.Groups["value"].Value, state), StringComparer.OrdinalIgnoreCase);
        var scriptAttrs = RenderScriptAttributes(attrs);

        var lines = new List<string>();
        foreach (var script in BuiltInScripts)
        {
            var path = attrs.TryGetValue(script.Name, out var overridePath) ? overridePath : script.Path;
            if (IsDisabledAsset(path))
                continue;

            path = SafeUrl(path);
            if (path == "#" || string.IsNullOrWhiteSpace(path))
                continue;

            lines.Add($"<script src=\"{Html(path)}\"{scriptAttrs}></script>");
        }

        foreach (var path in ParseExtraScripts(attrs.GetValueOrDefault("Extra")))
        {
            var safePath = SafeUrl(path);
            if (safePath == "#" || string.IsNullOrWhiteSpace(safePath))
                continue;

            lines.Add($"<script src=\"{Html(safePath)}\"{scriptAttrs}></script>");
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            var renderedContent = ScriptRegex.Replace(content, match => RenderScript(match.Groups["attrs"].Value, state, attrs));
            renderedContent = RenderBindings(renderedContent, state).Trim();
            if (!string.IsNullOrWhiteSpace(renderedContent))
                lines.Add(renderedContent);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string RenderScript(string rawAttributes, IReadOnlyDictionary<string, object?> state, IReadOnlyDictionary<string, string>? inheritedAttributes = null)
    {
        var attrs = inheritedAttributes is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(inheritedAttributes, StringComparer.OrdinalIgnoreCase);

        foreach (var match in AttributeRegex.Matches(rawAttributes).Cast<Match>())
        {
            attrs[match.Groups["name"].Value] = RenderBindings(match.Groups["value"].Value, state);
        }

        attrs.Remove("Extra");
        foreach (var script in BuiltInScripts)
        {
            attrs.Remove(script.Name);
        }

        if (!attrs.TryGetValue("Src", out var src) || string.IsNullOrWhiteSpace(src))
            return string.Empty;

        var safeSrc = SafeUrl(src);
        if (safeSrc == "#" || string.IsNullOrWhiteSpace(safeSrc))
            return string.Empty;

        return $"<script src=\"{Html(safeSrc)}\"{RenderScriptAttributes(attrs)}></script>";
    }

    private static string RenderScriptAttributes(IReadOnlyDictionary<string, string> attrs)
    {
        var output = new StringBuilder();
        foreach (var name in new[] { "Type", "Nonce", "Integrity", "Crossorigin", "Referrerpolicy" })
        {
            if (attrs.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
                output.Append(' ').Append(name.ToLowerInvariant()).Append("=\"").Append(Html(value)).Append('"');
        }

        foreach (var name in new[] { "Async", "Defer" })
        {
            if (attrs.TryGetValue(name, out var value) && IsTruthyAttribute(value))
                output.Append(' ').Append(name.ToLowerInvariant());
        }

        return output.ToString();
    }

    private static IEnumerable<string> ParseExtraScripts(string? raw)
    {
        return string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

    private static string RenderLayoutAttributes(string rawAttributes, IReadOnlyDictionary<string, object?> state, string className, string style, params string[] excluded)
    {
        var originalClass = RenderBindings(GetAttribute(rawAttributes, "class") ?? string.Empty, state);
        var originalStyle = RenderBindings(GetAttribute(rawAttributes, "style") ?? string.Empty, state);
        var attrs = RenderHtmlAttributes(rawAttributes, state, excluded.Concat(["class", "style"]).ToArray());
        var classes = string.Join(' ', new[] { className, originalClass }.Where(item => !string.IsNullOrWhiteSpace(item)));
        var styles = string.Join(' ', new[] { originalStyle, style }.Where(item => !string.IsNullOrWhiteSpace(item)));

        var output = new StringBuilder(attrs);
        if (!string.IsNullOrWhiteSpace(classes))
            output.Append(" class=\"").Append(Html(classes)).Append('"');
        if (!string.IsNullOrWhiteSpace(styles))
            output.Append(" style=\"").Append(Html(styles)).Append('"');

        return output.ToString();
    }

    private static string BuildGridClass(string columns)
    {
        if (int.TryParse(columns, out var count) && count is >= 2 and <= 3)
            return $" cs-grid-{count}";

        return string.Empty;
    }

    private static string BuildGapStyle(string gap)
    {
        if (string.IsNullOrWhiteSpace(gap))
            return string.Empty;

        var value = gap.Trim().ToLowerInvariant() switch
        {
            "xs" => "0.25rem",
            "sm" => "0.5rem",
            "md" => "1rem",
            "lg" => "1.5rem",
            "xl" => "2rem",
            _ => gap.Trim()
        };

        return $"gap: {value};";
    }

    private static string BuildAlignStyle(string align)
    {
        if (string.IsNullOrWhiteSpace(align))
            return string.Empty;

        var value = align.Trim().ToLowerInvariant() switch
        {
            "start" => "flex-start",
            "center" => "center",
            "end" => "flex-end",
            "stretch" => "stretch",
            _ => align.Trim()
        };

        return $"align-items: {value};";
    }

    private static string SafeClassToken(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), @"[^a-z0-9_-]", "-");
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

    private static string RenderCrudField(CrudField field, IReadOnlyDictionary<string, object?> state)
    {
        var value = Html(FormatValue(GetValue(state, field.Name), null));
        var id = Html(field.Name);
        var label = Html(field.Label);
        var name = Html(field.Name);

        if (field.Type.Equals("textarea", StringComparison.OrdinalIgnoreCase) || field.Rows is not null)
        {
            var rows = field.Rows is null ? string.Empty : $" rows=\"{Html(field.Rows)}\"";
            return $"<label for=\"{id}\">{label}<textarea id=\"{id}\" name=\"{name}\" data-cs-bind=\"{name}\"{rows}>{value}</textarea></label>";
        }

        return $"<label for=\"{id}\">{label}<input id=\"{id}\" type=\"{Html(field.Type)}\" name=\"{name}\" value=\"{value}\" data-cs-bind=\"{name}\" /></label>";
    }

    private static List<CrudField> ParseCrudFields(string? rawFields)
    {
        if (string.IsNullOrWhiteSpace(rawFields))
            return [];

        return rawFields.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', 4, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            .Select(parts => new CrudField(
                parts[0],
                parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : parts[0],
                parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? SafeInputType(parts[2]) : "text",
                parts.Length > 3 && !string.IsNullOrWhiteSpace(parts[3]) ? parts[3] : null))
            .ToList();
    }

    private static List<CrudCommand> ParseCrudCommands(string? rawCommands)
    {
        if (string.IsNullOrWhiteSpace(rawCommands))
            return [];

        return rawCommands.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', 3, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            .Select(parts => new CrudCommand(
                parts[0],
                parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : parts[0],
                parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null))
            .ToList();
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

    private static string RenderToolbar(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var title = GetAttribute(rawAttributes, "Title");
        var subtitle = GetAttribute(rawAttributes, "Subtitle");
        var attrs = RenderHtmlAttributes(rawAttributes, state, "Title", "Subtitle");
        var heading = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(subtitle))
        {
            heading.Append("<div class=\"cs-toolbar-title\">");
            if (!string.IsNullOrWhiteSpace(title))
                heading.Append("<h2>").Append(Html(RenderBindings(title, state))).Append("</h2>");
            if (!string.IsNullOrWhiteSpace(subtitle))
                heading.Append("<p>").Append(Html(RenderBindings(subtitle, state))).Append("</p>");
            heading.Append("</div>");
        }

        return $"<div class=\"cs-toolbar\"{attrs}>{heading}<div class=\"cs-toolbar-actions\">{content}</div></div>";
    }

    private static string RenderTabs(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var active = RenderBindings(GetAttribute(rawAttributes, "Active") ?? string.Empty, state);
        var attrs = RenderHtmlAttributes(rawAttributes, state, "Active");
        var tabs = TabRegex.Matches(content)
            .Select((match, index) =>
            {
                var raw = match.Groups["attrs"].Value;
                var key = RenderBindings(GetAttribute(raw, "Key") ?? $"tab-{index + 1}", state);
                var title = RenderBindings(GetAttribute(raw, "Title") ?? key, state);
                var selected = string.IsNullOrWhiteSpace(active) ? index == 0 : string.Equals(active, key, StringComparison.OrdinalIgnoreCase);
                return new TabItem(key, title, selected, match.Groups["content"].Value);
            })
            .ToList();

        if (tabs.Count == 0)
            return string.Empty;

        var nav = string.Concat(tabs.Select(tab => $"<a href=\"#{Html(tab.Key)}\" class=\"cs-tab{(tab.Selected ? " active" : string.Empty)}\" role=\"tab\" aria-selected=\"{tab.Selected.ToString().ToLowerInvariant()}\">{Html(tab.Title)}</a>"));
        var panels = string.Concat(tabs.Select(tab => $"<section id=\"{Html(tab.Key)}\" class=\"cs-tab-panel{(tab.Selected ? " active" : string.Empty)}\" role=\"tabpanel\">{tab.Content}</section>"));
        return $"<div class=\"cs-tabs\" data-ui=\"tabs\"{attrs}><nav class=\"cs-tab-list\" role=\"tablist\">{nav}</nav>{panels}</div>";
    }

    private static string RenderModal(string rawAttributes, string content, IReadOnlyDictionary<string, object?> state)
    {
        var id = RenderBindings(GetAttribute(rawAttributes, "Id") ?? "cs-modal", state);
        var title = RenderBindings(GetAttribute(rawAttributes, "Title") ?? string.Empty, state);
        var open = IsTruthyAttribute(RenderBindings(GetAttribute(rawAttributes, "Open") ?? string.Empty, state));
        var attrs = RenderHtmlAttributes(rawAttributes, state, "Id", "Title", "Open");
        var hidden = open ? string.Empty : " hidden";
        var titleHtml = string.IsNullOrWhiteSpace(title) ? string.Empty : $"<header class=\"cs-modal-header\"><h2>{Html(title)}</h2></header>";
        return $"<div id=\"{Html(id)}\" class=\"cs-modal\" data-ui=\"modal\" role=\"dialog\" aria-modal=\"true\"{hidden}{attrs}><div class=\"cs-modal-panel\">{titleHtml}<div class=\"cs-modal-body\">{content}</div></div></div>";
    }

    private static string RenderPager(string rawAttributes, IReadOnlyDictionary<string, object?> state)
    {
        var page = Math.Max(1, ParsePositiveInt(RenderBindings(GetAttribute(rawAttributes, "Page") ?? "1", state), 1));
        var totalPages = Math.Max(1, ParsePositiveInt(RenderBindings(GetAttribute(rawAttributes, "TotalPages") ?? "1", state), 1));
        var url = RenderBindings(GetAttribute(rawAttributes, "Url") ?? "?page={page}", state);
        var attrs = RenderHtmlAttributes(rawAttributes, state, "Page", "TotalPages", "Url");
        var links = new StringBuilder();

        links.Append(RenderPagerLink(url, Math.Max(1, page - 1), "Previous", page <= 1));
        for (var index = 1; index <= totalPages; index++)
        {
            links.Append(RenderPagerLink(url, index, index.ToString(), false, index == page));
        }
        links.Append(RenderPagerLink(url, Math.Min(totalPages, page + 1), "Next", page >= totalPages));

        return $"<nav class=\"cs-pager\" aria-label=\"Pagination\"{attrs}>{links}</nav>";
    }

    private static string? ExtractBindingName(string binding)
    {
        var match = BindingRegex.Match(binding);
        return match.Success ? match.Groups["name"].Value : binding;
    }

    private static System.Collections.IEnumerable? GetEnumerableFromBinding(string rawAttributes, string attributeName, IReadOnlyDictionary<string, object?> state)
    {
        var binding = GetAttribute(rawAttributes, attributeName);
        if (string.IsNullOrWhiteSpace(binding))
            return null;

        var name = ExtractBindingName(binding);
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var value = GetValue(state, name);
        return value is System.Collections.IEnumerable items && value is not string ? items : null;
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

    private static int ParsePositiveInt(string value, int fallback)
    {
        return int.TryParse(value, out var result) && result > 0 ? result : fallback;
    }

    private static string RenderPagerLink(string template, int page, string label, bool disabled, bool active = false)
    {
        var href = SafeUrl(template.Replace("{page}", page.ToString(), StringComparison.OrdinalIgnoreCase));
        var classes = new List<string> { "cs-pager-link" };
        if (active)
            classes.Add("active");
        if (disabled)
            classes.Add("disabled");

        var ariaCurrent = active ? " aria-current=\"page\"" : string.Empty;
        var ariaDisabled = disabled ? " aria-disabled=\"true\"" : string.Empty;
        return disabled
            ? $"<span class=\"{string.Join(' ', classes)}\"{ariaDisabled}>{Html(label)}</span>"
            : $"<a class=\"{string.Join(' ', classes)}\" href=\"{Html(href)}\"{ariaCurrent}>{Html(label)}</a>";
    }

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

    private static string SafeInputType(string? type)
    {
        var value = (type ?? "text").Trim().ToLowerInvariant();
        return Regex.IsMatch(value, @"^[a-z][a-z0-9_-]*$") ? value : "text";
    }

    private static bool IsDisabledAsset(string? value)
    {
        return string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTruthyAttribute(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(value, "off", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
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

    private sealed record CrudField(string Name, string Label, string Type, string? Rows);

    private sealed record CrudCommand(string Name, string Text, string? Confirm);

    private sealed record TabItem(string Key, string Title, bool Selected, string Content);

    private sealed record ScriptAsset(string Name, string Path);

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
