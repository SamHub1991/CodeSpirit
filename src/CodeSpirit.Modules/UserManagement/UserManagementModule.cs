using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using CodeSpirit.Infrastructure.Authentication;
using CodeSpirit.Infrastructure.Caching;
using CodeSpirit.Infrastructure.EntityFramework;
using CodeSpirit.Modules.UserManagement.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Modules.UserManagement;

[DependsOn(typeof(EfCoreModule))]
[DependsOn(typeof(JwtAuthModule))]
[DependsOn(typeof(CachingModule))]
[ConfigurationProfile("default,development")]
public class UserManagementModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IUserService, UserService>();
    }

    public override void Configure(IServiceProvider serviceProvider)
    {
    }
}
