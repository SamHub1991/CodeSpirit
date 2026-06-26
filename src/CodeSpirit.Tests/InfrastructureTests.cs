using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Interfaces;
using CodeSpirit.Infrastructure.AutoConfiguration;
using CodeSpirit.Infrastructure.Aop;
using CodeSpirit.Infrastructure.Page;
using CodeSpirit.Infrastructure;
using CodeSpirit.Infrastructure.Mvvm;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;
using System.Reflection;

namespace CodeSpirit.Tests;

public class AopExtensionsTests
{
    [Fact]
    public void NeedsAopProxy_WithTransactionalMethod_ReturnsTrue()
    {
        Assert.True(AopExtensions.NeedsAopProxy(typeof(TransactionalService)));
    }

    [Fact]
    public void NeedsAopProxy_WithCacheableMethod_ReturnsTrue()
    {
        Assert.True(AopExtensions.NeedsAopProxy(typeof(CacheableService)));
    }

    [Fact]
    public void NeedsAopProxy_WithoutAopAttributes_ReturnsFalse()
    {
        Assert.False(AopExtensions.NeedsAopProxy(typeof(PlainService)));
    }

    [Fact]
    public void NeedsAopProxy_WithBothAttributes_ReturnsTrue()
    {
        Assert.True(AopExtensions.NeedsAopProxy(typeof(MixedAopService)));
    }

    [Service]
    class TransactionalService
    {
        [Transactional]
        public virtual void DoWork() { }
    }

    [Service]
    class CacheableService
    {
        [Cacheable(ExpirationSeconds = 60, CacheKey = "test")]
        public virtual int GetValue() => 42;
    }

    [Service]
    class PlainService
    {
        public void DoWork() { }
    }

    [Service]
    class MixedAopService
    {
        [Transactional]
        public virtual void Save() { }

        [Cacheable]
        public virtual string Get() => "cached";
    }
}

public class ModuleLoaderTests
{
    private readonly ModuleLoader _loader = new();

    [Fact]
    public void ResolveModuleTopology_SingleModule_ReturnsIt()
    {
        var types = _loader.ResolveModuleTopology(typeof(SingleModule).Assembly);
        Assert.Contains(typeof(SingleModule), types);
    }

    [Fact]
    public void ResolveModuleTopology_DependencyOrderedCorrectly()
    {
        var types = _loader.ResolveModuleTopology(typeof(ParentModule).Assembly);

        var parentIdx = types.IndexOf(typeof(ParentModule));
        var childIdx = types.IndexOf(typeof(ChildModule));

        Assert.True(childIdx < parentIdx, "ChildModule should be resolved before ParentModule because ParentModule depends on it");
    }

    [Fact]
    public void ResolveModuleTopology_NoDependencies_ReturnsStandalone()
    {
        var types = _loader.ResolveModuleTopology(typeof(StandaloneModule).Assembly);
        Assert.Contains(typeof(StandaloneModule), types);
    }

    [Fact]
    public void ResolveModuleTopology_ChainDependency_PreservesOrder()
    {
        var types = _loader.ResolveModuleTopology(typeof(GrandparentModule).Assembly);

        var grandparentIdx = types.IndexOf(typeof(GrandparentModule));
        var parentIdx = types.IndexOf(typeof(ParentChainModule));
        var childIdx = types.IndexOf(typeof(ChildChainModule));

        Assert.True(childIdx < parentIdx);
        Assert.True(parentIdx < grandparentIdx);
    }
}

public class AutoServiceRegistrarTests
{
    private readonly AutoServiceRegistrar _registrar = new();

    [Fact]
    public void RegisterServices_WithServiceAttribute_RegistersService()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        _registrar.RegisterServices(services, typeof(SampleService).Assembly);

        var descriptor = services.FirstOrDefault(s =>
            s.ServiceType == typeof(ISampleService) || s.ServiceType == typeof(SampleService));
        Assert.NotNull(descriptor);
    }

    [Fact]
    public void RegisterServices_WithRepositoryAttribute_RegistersRepository()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        _registrar.RegisterServices(services, typeof(SampleRepository).Assembly);

        var descriptor = services.FirstOrDefault(s =>
            s.ImplementationType == typeof(SampleRepository));
        Assert.NotNull(descriptor);
    }
}

// Test module classes for ModuleLoader tests
public class SingleModule : CodeSpiritModule { }

[DependsOn(typeof(ChildModule))]
public class ParentModule : CodeSpiritModule { }

public class ChildModule : CodeSpiritModule { }

public class StandaloneModule : CodeSpiritModule { }

[DependsOn(typeof(ParentChainModule))]
public class GrandparentModule : CodeSpiritModule { }

[DependsOn(typeof(ChildChainModule))]
public class ParentChainModule : CodeSpiritModule { }

public class ChildChainModule : CodeSpiritModule { }

// Test service classes for AutoServiceRegistrar tests
public interface ISampleService { }

[Service(ServiceType = typeof(ISampleService))]
public class SampleService : ISampleService { }

[Repository]
public class SampleRepository { }

public class ShowTagTests
{
    [Fact]
    public void Show_Visible_GeneratesDataCsVisible()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Visible=\"{Binding IsActive}\">Hello</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("data-cs-visible=\"IsActive\"", html);
        Assert.Contains(">Hello<", html);
    }

    [Fact]
    public void Show_HideWhen_GeneratesDataCsHidden()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show HideWhen=\"{Binding IsChecked}\">Content</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("data-cs-hidden=\"IsChecked\"", html);
    }

    [Fact]
    public void Show_Class_GeneratesDataCsClass()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Class=\"{Binding IsActive}\" ClassName=\"active\">Item</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("data-cs-class=\"IsActive:active\"", html);
    }

    [Fact]
    public void Show_ClassWithoutClassName_UsesFieldName()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Class=\"{Binding IsActive}\">Item</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("data-cs-class=\"IsActive\"", html);
    }

    [Fact]
    public void Show_TagAttribute_RespectsCustomTag()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Visible=\"{Binding Ready}\" Tag=\"span\">Label</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("<span", html);
        Assert.Contains("</span>", html);
    }

    [Fact]
    public void Show_InvalidTagAttribute_FallsBackToDiv()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Visible=\"{Binding Ready}\" Tag=\"1script\">Label</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("<div", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void Show_DefaultTag_IsDiv()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Visible=\"{Binding Ready}\">Label</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("<div", html);
    }

    [Fact]
    public void Show_MultipleBindings_GeneratesAllAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Visible=\"{Binding Active}\" Class=\"{Binding Theme}\" ClassName=\"dark\">Box</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("data-cs-visible=\"Active\"", html);
        Assert.Contains("data-cs-class=\"Theme:dark\"", html);
    }

    [Fact]
    public void Show_PassesThroughExtraAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Visible=\"{Binding Ready}\" Id=\"panel\" Style=\"display:block\">Body</cs:Show>",
            new Dictionary<string, object?>());

        Assert.Contains("Id=\"panel\"", html);
        Assert.Contains("Style=\"display:block\"", html);
    }

    [Fact]
    public void Show_DropsUnsafeEventAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Show Visible=\"{Binding Ready}\" onclick=\"alert(1)\" srcdoc=\"<p>x</p>\">Body</cs:Show>",
            new Dictionary<string, object?>());

        Assert.DoesNotContain("onclick", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("srcdoc", html, StringComparison.OrdinalIgnoreCase);
    }
}

public class PageParserTests
{
    [Fact]
    public void ParseFromFile_ReadsStandardAspxPageDirective()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"codespirit-page-{Guid.NewGuid():N}.aspx");
        File.WriteAllText(filePath, "<%@ Page Route=\"/customers\" Title=\"Customer Portal\" Layout=\"Pages/Customer.master\" %>");

        var descriptor = new PageParser().ParseFromFile(filePath);

        Assert.NotNull(descriptor);
        Assert.Equal("/customers", descriptor.Route);
        Assert.Equal("Customer Portal", descriptor.Title);
        Assert.Equal("Pages/Customer.master", descriptor.Layout);
    }

    [Fact]
    public void ParseFromFile_IgnoresFilesWithoutPageDirective()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"codespirit-page-{Guid.NewGuid():N}.aspx");
        File.WriteAllText(filePath, "<div>No directive</div>");

        var descriptor = new PageParser().ParseFromFile(filePath);

        Assert.Null(descriptor);
    }
}

public class PageRendererTagTests
{
    [Fact]
    public void Region_RendersDataRegionAndKeepsAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Region Name=\"orders\" Tag=\"section\" class=\"panel\">Body</cs:Region>",
            new Dictionary<string, object?>());

        Assert.Contains("<section data-cs-region=\"orders\" class=\"panel\">Body</section>", html);
    }

    [Fact]
    public void Region_InvalidTagAttribute_FallsBackToDiv()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Region Name=\"orders\" Tag=\"1script\">Body</cs:Region>",
            new Dictionary<string, object?>());

        Assert.Contains("<div data-cs-region=\"orders\">Body</div>", html);
        Assert.DoesNotContain("<script", html);
    }

    [Fact]
    public void FormFieldButton_RenderRuntimeConnectionAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Form class=\"search\"><cs:Field Name=\"Query\" Label=\"Search\" Placeholder=\"Keyword\" /><cs:Button Command=\"Search\">Go</cs:Button></cs:Form>",
            new Dictionary<string, object?> { ["Query"] = "clean" });

        Assert.Contains("<form method=\"post\" data-cs-vm class=\"search\">", html);
        Assert.Contains("name=\"Query\"", html);
        Assert.Contains("value=\"clean\"", html);
        Assert.Contains("data-cs-bind=\"Query\"", html);
        Assert.Contains("data-cs-command=\"Search\"", html);
    }

    [Fact]
    public void Link_DropsUnsafeJavascriptUrl()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Link NavigateTo=\"javascript:alert(1)\">Bad</cs:Link>",
            new Dictionary<string, object?>());

        Assert.Contains("href=\"#\"", html);
        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Form_InvalidMethod_FallsBackToPost()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Form Method=\"dialog\">Body</cs:Form>",
            new Dictionary<string, object?>());

        Assert.Contains("<form method=\"post\" data-cs-vm>", html);
    }

    [Fact]
    public void Scripts_RendersDefaultBuiltInAssets()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Scripts />",
            new Dictionary<string, object?>());

        Assert.Contains("/js/codespirit.runtime.js", html);
        Assert.Contains("/js/vendor/jquery-lite.js", html);
        Assert.Contains("/js/ui/ui.behaviors.js", html);
        Assert.Contains("/js/ui/codespirit.intent.js", html);
        Assert.Contains("/js/ui/codespirit.devpanel.js", html);
        Assert.Contains("/js/site.js", html);
    }

    [Fact]
    public void Scripts_AllowsAssetReplacementAndDisable()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Scripts Runtime=\"/custom/runtime.js\" DevPanel=\"none\" />",
            new Dictionary<string, object?>());

        Assert.Contains("/custom/runtime.js", html);
        Assert.DoesNotContain("/js/codespirit.runtime.js", html);
        Assert.DoesNotContain("/js/ui/codespirit.devpanel.js", html);
    }

    [Fact]
    public void Scripts_AppendsExtraScriptsAfterBuiltInsAndAppliesAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Scripts Defer=\"true\" Type=\"module\" Extra=\"/js/pages/home.js,/js/custom.js\" />",
            new Dictionary<string, object?>());

        Assert.Contains("<script src=\"/js/codespirit.runtime.js\" type=\"module\" defer></script>", html);
        Assert.Contains("<script src=\"/js/pages/home.js\" type=\"module\" defer></script>", html);
        Assert.Contains("<script src=\"/js/custom.js\" type=\"module\" defer></script>", html);
        Assert.True(html.IndexOf("/js/site.js", StringComparison.Ordinal) < html.IndexOf("/js/pages/home.js", StringComparison.Ordinal));
    }

    [Fact]
    public void Scripts_BlockAppendsNestedPageScriptsAfterBuiltIns()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Scripts Defer=\"true\"><cs:Script Src=\"/js/pages/{Binding Page}.js\" /></cs:Scripts>",
            new Dictionary<string, object?> { ["Page"] = "home" });

        Assert.Contains("<script src=\"/js/codespirit.runtime.js\" defer></script>", html);
        Assert.Contains("<script src=\"/js/pages/home.js\" defer></script>", html);
        Assert.True(html.IndexOf("/js/site.js", StringComparison.Ordinal) < html.IndexOf("/js/pages/home.js", StringComparison.Ordinal));
    }

    [Fact]
    public void Scripts_NestedScriptCanOverrideInheritedAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Scripts Type=\"module\" Defer=\"true\"><cs:Script Src=\"/js/pages/home.js\" Type=\"text/javascript\" Defer=\"false\" /></cs:Scripts>",
            new Dictionary<string, object?>());

        Assert.Contains("<script src=\"/js/codespirit.runtime.js\" type=\"module\" defer></script>", html);
        Assert.Contains("<script src=\"/js/pages/home.js\" type=\"text/javascript\"></script>", html);
    }

    [Fact]
    public void Scripts_BlockPreservesRenderedRawPageScriptsAfterBuiltIns()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Scripts><script src=\"/js/pages/{Binding Page}.js\"></script></cs:Scripts>",
            new Dictionary<string, object?> { ["Page"] = "home" });

        Assert.Contains("<script src=\"/js/pages/home.js\"></script>", html);
        Assert.True(html.IndexOf("/js/site.js", StringComparison.Ordinal) < html.IndexOf("/js/pages/home.js", StringComparison.Ordinal));
    }

    [Fact]
    public void Scripts_DropsUnsafeExtraScriptUrls()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Scripts Extra=\"javascript:alert(1),/js/safe.js\" />",
            new Dictionary<string, object?>());

        Assert.DoesNotContain("javascript:", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/js/safe.js", html);
    }

    [Fact]
    public void Script_RendersSafePageScriptWithAttributes()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Script Src=\"/js/pages/home.js\" Defer=\"true\" />",
            new Dictionary<string, object?>());

        Assert.Equal("<script src=\"/js/pages/home.js\" defer></script>", html);
    }

    [Fact]
    public void Script_DropsUnsafeSrc()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Script Src=\"javascript:alert(1)\" />",
            new Dictionary<string, object?>());

        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void Table_ColumnListInfersIntentForRuntimeStyling()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Table Items=\"{Binding Rows}\" Columns=\"Name:Name,Status:Status,FineBalance:Fine\" />",
            new Dictionary<string, object?>
            {
                ["Rows"] = new[] { new TableRow("Ada", "Active", 12.5m) }
            });

        Assert.Contains("<th>Name</th>", html);
        Assert.Contains("<span data-cs-intent=\"status\">Active</span>", html);
        Assert.Contains("<span data-cs-intent=\"numeric\">12.5</span>", html);
    }

    [Fact]
    public void Table_BlockColumnsRenderTemplates()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Table Items=\"{Binding Rows}\"><cs:Column Header=\"Status\"><span class=\"status-{Binding Status}\">{Binding Status}</span></cs:Column></cs:Table>",
            new Dictionary<string, object?>
            {
                ["Rows"] = new[] { new TableRow("Ada", "Active", 12.5m) }
            });

        Assert.Contains("<th>Status</th>", html);
        Assert.Contains("<span class=\"status-Active\">Active</span>", html);
    }

    [Fact]
    public void Toolbar_RendersTitleSubtitleAndActions()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Toolbar Title=\"{Binding Title}\" Subtitle=\"Manage records\"><button>Add</button></cs:Toolbar>",
            new Dictionary<string, object?> { ["Title"] = "Books" });

        Assert.Contains("class=\"cs-toolbar\"", html);
        Assert.Contains("<h2>Books</h2>", html);
        Assert.Contains("<p>Manage records</p>", html);
        Assert.Contains("<div class=\"cs-toolbar-actions\"><button>Add</button></div>", html);
    }

    [Fact]
    public void Tabs_RendersActiveTabAndPanels()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Tabs Active=\"loans\"><cs:Tab Key=\"overview\" Title=\"Overview\">One</cs:Tab><cs:Tab Key=\"loans\" Title=\"Loans\">Two</cs:Tab></cs:Tabs>",
            new Dictionary<string, object?>());

        Assert.Contains("role=\"tablist\"", html);
        Assert.Contains("href=\"#loans\" class=\"cs-tab active\"", html);
        Assert.Contains("id=\"loans\" class=\"cs-tab-panel active\"", html);
        Assert.Contains("id=\"overview\" class=\"cs-tab-panel\"", html);
    }

    [Fact]
    public void Modal_RendersDialogAndHiddenState()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Modal Id=\"edit-book\" Title=\"Edit Book\">Body</cs:Modal>",
            new Dictionary<string, object?>());

        Assert.Contains("id=\"edit-book\"", html);
        Assert.Contains("role=\"dialog\"", html);
        Assert.Contains(" hidden", html);
        Assert.Contains("<h2>Edit Book</h2>", html);
    }

    [Fact]
    public void Pager_RendersLinksAndCurrentPage()
    {
        var html = PageRenderer.RenderTemplateForTests(
            "<cs:Pager Page=\"2\" TotalPages=\"3\" Url=\"/admin?page={page}\" />",
            new Dictionary<string, object?>());

        Assert.Contains("aria-label=\"Pagination\"", html);
        Assert.Contains("href=\"/admin?page=1\"", html);
        Assert.Contains("href=\"/admin?page=3\"", html);
        Assert.Contains("class=\"cs-pager-link active\" href=\"/admin?page=2\" aria-current=\"page\"", html);
    }

    private sealed record TableRow(string Name, string Status, decimal FineBalance);
}

public class ValueConverterTests
{
    [Fact]
    public void ConvertValue_NullToNonNullableValueType_ReturnsDefault()
    {
        var value = ValueConverter.ConvertValue(null, typeof(int));

        Assert.Equal(0, value);
    }

    [Fact]
    public void ConvertValue_EmptyStringToNullableValueType_ReturnsNull()
    {
        var value = ValueConverter.ConvertValue(string.Empty, typeof(int?));

        Assert.Null(value);
    }

    [Fact]
    public void ConvertValue_StringToEnum_IgnoresCase()
    {
        var value = ValueConverter.ConvertValue("green", typeof(TestTone));

        Assert.Equal(TestTone.Green, value);
    }

    private enum TestTone
    {
        Green,
        Red
    }
}

public class ViewModelExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_PostJson_BindsTwoWayPropertyAndRunsCommand()
    {
        var ctx = CreateContext("POST", "{\"Count\":3,\"__command\":\"Increment\"}");
        var executor = CreateExecutor(ctx.RequestServices);

        await executor.ExecuteAsync(ctx, typeof(ExecutorTestViewModel));

        var response = await ReadJsonResponse(ctx);

        Assert.Equal(200, ctx.Response.StatusCode);
        Assert.Equal(4, response.RootElement.GetProperty("state").GetProperty("Count").GetInt32());
        Assert.Contains("Increment", response.RootElement.GetProperty("commands").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task ExecuteAsync_PostJson_UnknownCommand_ReturnsJsonError()
    {
        var ctx = CreateContext("POST", "{\"__command\":\"Missing\"}");
        var executor = CreateExecutor(ctx.RequestServices);

        await executor.ExecuteAsync(ctx, typeof(ExecutorTestViewModel));

        var response = await ReadJsonResponse(ctx);

        Assert.Equal(500, ctx.Response.StatusCode);
        Assert.Contains("Command 'Missing' was not found", response.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_Get_RendersHtmlFallbackWhenPageFileMissing()
    {
        var ctx = CreateContext("GET", string.Empty);
        var executor = CreateExecutor(ctx.RequestServices);

        await executor.ExecuteAsync(ctx, typeof(ExecutorTestViewModel));

        ctx.Response.Body.Position = 0;
        var html = await new StreamReader(ctx.Response.Body).ReadToEndAsync();

        Assert.Equal(200, ctx.Response.StatusCode);
        Assert.Equal("text/html; charset=utf-8", ctx.Response.ContentType);
        Assert.Contains("Executor Test", html);
        Assert.Contains("Count", html);
    }

    [Fact]
    public async Task ExecuteAsync_Get_EncodesFallbackTitle()
    {
        var ctx = CreateContext("GET", string.Empty);
        var executor = CreateExecutor(ctx.RequestServices);

        await executor.ExecuteAsync(ctx, typeof(UnsafeTitleViewModel));

        ctx.Response.Body.Position = 0;
        var html = await new StreamReader(ctx.Response.Body).ReadToEndAsync();

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
    }

    [Fact]
    public async Task ExecuteAsync_Get_BindsQueryAndRouteValuesBeforeRendering()
    {
        EnsureTestPage("ExecutorQueryRoute.aspx", """
<%@ Page Title="Query Route Test" %>
<p>Query: {Binding Query}</p>
<p>RouteId: {Binding RouteId}</p>
""");
        var ctx = CreateContext("GET", string.Empty);
        ctx.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["q"] = "books"
        });
        ctx.GetRouteData().Values["id"] = "42";
        var executor = CreateExecutor(ctx.RequestServices);

        await executor.ExecuteAsync(ctx, typeof(ExecutorQueryRouteViewModel));

        ctx.Response.Body.Position = 0;
        var html = await new StreamReader(ctx.Response.Body).ReadToEndAsync();

        Assert.Contains("Query: books", html);
        Assert.Contains("RouteId: 42", html);
    }

    private static void EnsureTestPage(string fileName, string content)
    {
        var pagesDir = Path.Combine(AppContext.BaseDirectory, "Pages");
        Directory.CreateDirectory(pagesDir);
        File.WriteAllText(Path.Combine(pagesDir, fileName), content, Encoding.UTF8);
    }

    private static DefaultHttpContext CreateContext(string method, string body)
    {
        var services = new ServiceCollection();
        services.AddScoped<ExecutorTestViewModel>();
        services.AddScoped<ExecutorQueryRouteViewModel>();
        services.AddScoped<UnsafeTitleViewModel>();
        var provider = services.BuildServiceProvider();

        var ctx = new DefaultHttpContext { RequestServices = provider };
        ctx.Request.Method = method;
        ctx.Response.Body = new MemoryStream();

        if (!string.IsNullOrEmpty(body))
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            ctx.Request.Body = new MemoryStream(bytes);
            ctx.Request.ContentLength = bytes.Length;
            ctx.Request.ContentType = "application/json";
        }

        return ctx;
    }

    private static ViewModelExecutor CreateExecutor(IServiceProvider services)
    {
        var renderer = new PageRenderer(new PageParser(), NullLogger<PageRenderer>.Instance);
        return new ViewModelExecutor(NullLogger<ViewModelExecutor>.Instance, renderer);
    }

    private static async Task<JsonDocument> ReadJsonResponse(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(ctx.Response.Body);
    }

    [CodeSpirit.Core.Page.PageDirective(Title = "Executor Test")]
    private sealed class ExecutorTestViewModel : CodeSpirit.Core.ViewModel
    {
        [CodeSpirit.Core.Mvvm.Bind(CodeSpirit.Core.Mvvm.BindDirection.TwoWay)]
        public int Count { get; set; }

        [CodeSpirit.Core.Mvvm.Command]
        public void Increment()
        {
            Count++;
        }
    }

    [CodeSpirit.Core.Page.PageDirective(Title = "Query Route Test")]
    private sealed class ExecutorQueryRouteViewModel : CodeSpirit.Core.ViewModel
    {
        [CodeSpirit.Core.Mvvm.FromQuery("q")]
        [CodeSpirit.Core.Mvvm.Bind]
        public string Query { get; set; } = string.Empty;

        [CodeSpirit.Core.Mvvm.FromRoute("id")]
        [CodeSpirit.Core.Mvvm.Bind]
        public int RouteId { get; set; }
    }

    [CodeSpirit.Core.Page.PageDirective(Title = "<script>alert(1)</script>")]
    private sealed class UnsafeTitleViewModel : CodeSpirit.Core.ViewModel
    {
        [CodeSpirit.Core.Mvvm.Bind]
        public string Message { get; set; } = "safe";
    }
}
