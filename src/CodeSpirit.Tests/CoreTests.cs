using CodeSpirit.Core;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;

namespace CodeSpirit.Tests;

public class AttributeTests
{
    [Fact]
    public void ServiceAttribute_DefaultsToScoped()
    {
        var attr = new ServiceAttribute();
        Assert.Equal(ServiceLifetime.Scoped, attr.Lifetime);
    }

    [Fact]
    public void ServiceAttribute_CanSetServiceType()
    {
        var attr = new ServiceAttribute { ServiceType = typeof(IDisposable) };
        Assert.Equal(typeof(IDisposable), attr.ServiceType);
    }
}

public class RepositoryAttribute_Tests
{
    [Fact]
    public void RepositoryAttribute_CanApplyToClass()
    {
        var attr = Attribute.GetCustomAttribute(typeof(FakeRepo), typeof(RepositoryAttribute));
        Assert.NotNull(attr);
    }

    [Repository]
    class FakeRepo;
}

public class AutowiredAttribute_Tests
{
    [Fact]
    public void AutowiredAttribute_TargetsPropertyAndField()
    {
        var usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(AutowiredAttribute), typeof(AttributeUsageAttribute));
        Assert.NotNull(usage);
    }
}

public class ValueAttribute_Tests
{
    [Fact]
    public void ValueAttribute_StoresKey()
    {
        var attr = new ValueAttribute("Config:Key");
        Assert.Equal("Config:Key", attr.Key);
    }
}

public class ScheduledAttribute_Tests
{
    [Fact]
    public void ScheduledAttribute_StoresCronAndName()
    {
        var attr = new ScheduledAttribute("0 */5 * * * ?") { Name = "MyJob" };
        Assert.Equal("0 */5 * * * ?", attr.Cron);
        Assert.Equal("MyJob", attr.Name);
    }

    [Fact]
    public void EveryAttribute_StoresInterval()
    {
        var attr = new EveryAttribute(60);
        Assert.Equal(60, attr.Seconds);
    }

    [Fact]
    public void OnStartupAttribute_StoresDelay()
    {
        var attr = new OnStartupAttribute(2000);
        Assert.Equal(2000, attr.DelayMs);
    }
}

public class HttpAttribute_Tests
{
    [Fact]
    public void HttpGetAttribute_StoresPath()
    {
        var attr = new HttpGetAttribute("/api/users");
        Assert.Equal("/api/users", attr.Url);
    }

    [Fact]
    public void HttpPostAttribute_StoresPath()
    {
        var attr = new HttpPostAttribute("/api/users");
        Assert.Equal("/api/users", attr.Url);
    }
}

public class PageDirectiveAttribute_Tests
{
    [Fact]
    public void PageDirectiveAttribute_StoresRouteAndTitle()
    {
        var attr = new PageDirectiveAttribute { Route = "/weather", Title = "Weather" };
        Assert.Equal("/weather", attr.Route);
        Assert.Equal("Weather", attr.Title);
    }
}

public class BindAttribute_Tests
{
    [Fact]
    public void BindAttribute_DefaultsToOneWay()
    {
        var attr = new BindAttribute();
        Assert.Equal(BindDirection.OneWay, attr.Direction);
    }

    [Fact]
    public void BindAttribute_CanConfigureNameAndDirection()
    {
        var attr = new BindAttribute { Name = "city", Direction = BindDirection.TwoWay };
        Assert.Equal("city", attr.Name);
        Assert.Equal(BindDirection.TwoWay, attr.Direction);
    }
}

public class CommandAttribute_Tests
{
    [Fact]
    public void CommandAttribute_StoresName()
    {
        var attr = new CommandAttribute("Save");
        Assert.Equal("Save", attr.Name);
    }
}

public class DependsOnAttribute_Tests
{
    [Fact]
    public void DependsOnAttribute_StoresModuleTypes()
    {
        var attr = new DependsOnAttribute(typeof(AutowiredAttribute), typeof(ValueAttribute));
        Assert.Equal(2, attr.ModuleTypes.Length);
        Assert.Equal(typeof(AutowiredAttribute), attr.ModuleTypes[0]);
    }
}

public class TransactionalAttribute_Tests
{
    [Fact]
    public void TransactionalAttribute_TargetsMethod()
    {
        var usage = (AttributeUsageAttribute?)Attribute.GetCustomAttribute(
            typeof(TransactionalAttribute), typeof(AttributeUsageAttribute));
        Assert.NotNull(usage);
    }
}

public class CacheableAttribute_Tests
{
    [Fact]
    public void CacheableAttribute_DefaultsTo300Seconds()
    {
        var attr = new CacheableAttribute();
        Assert.Equal(300, attr.ExpirationSeconds);
    }

    [Fact]
    public void CacheableAttribute_CanSetKey()
    {
        var attr = new CacheableAttribute { CacheKey = "users:{0}" };
        Assert.Equal("users:{0}", attr.CacheKey);
    }
}

public class AssembliesTests
{
    [Fact]
    public void Assemblies_All_ReturnsAtLeastOne()
    {
        Assert.NotEmpty(CodeSpirit.Core.Assemblies.All);
    }

    [Fact]
    public void Assemblies_CodeSpirit_ContainsCoreAndEntry()
    {
        var names = CodeSpirit.Core.Assemblies.CodeSpirit
            .Select(a => a.GetName().Name)
            .ToList();

        Assert.Contains(names, n => n == "CodeSpirit.Core");
        Assert.Contains(names, n => n == "CodeSpirit.Tests");
    }
}

public class ViewModelTests
{
    [Fact]
    public async Task ViewModel_ToState_ReturnsBindProperties()
    {
        var vm = new TestViewModel();
        await vm.InitAsync();
        await vm.LoadAsync();
        await vm.RenderAsync();

        var state = vm.ToState();
        Assert.Equal("hello", state["Greeting"]);
    }

    [Fact]
    public void ViewModel_ToResponse_ReturnsBindingsAndCommands()
    {
        var vm = new TestViewModel { Greeting = "hello" };

        var response = vm.ToResponse();

        Assert.Equal("hello", response.State["Greeting"]);
        Assert.Equal("TwoWay", response.Bindings["Greeting"].Direction);
        Assert.Contains("Refresh", response.Commands);
    }
}

class TestViewModel : ViewModel
{
    [Bind(BindDirection.TwoWay)]
    public string Greeting { get; set; } = "";

    [Command]
    public void Refresh()
    {
        Greeting = "refreshed";
    }

    public override Task LoadAsync()
    {
        Greeting = "hello";
        return Task.CompletedTask;
    }
}
