using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Interfaces;
using CodeSpirit.Infrastructure.AutoConfiguration;
using CodeSpirit.Infrastructure.Aop;
using CodeSpirit.Infrastructure.Page;
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
}
