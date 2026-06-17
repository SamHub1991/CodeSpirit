using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Authentication;

[ConditionalOnClass("Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler")]
public class JwtAuthModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddCodeSpiritJwtAuth(context.Configuration);
    }
}
