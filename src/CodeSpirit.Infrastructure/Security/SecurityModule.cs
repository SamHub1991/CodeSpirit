using CodeSpirit.Core.Abstractions;
using CodeSpirit.Infrastructure.Authentication;

namespace CodeSpirit.Infrastructure.Security;

public class SecurityModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddCodeSpiritJwtAuth(context.Configuration);
    }
}
