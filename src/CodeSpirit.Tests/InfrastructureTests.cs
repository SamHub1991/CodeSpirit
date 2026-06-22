using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Interfaces;
using CodeSpirit.Infrastructure.AutoConfiguration;
using CodeSpirit.Infrastructure.Aop;
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
