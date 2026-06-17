using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Redis;

[ConditionalOnClass("StackExchange.Redis.ConnectionMultiplexer, StackExchange.Redis")]
public class RedisCacheModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddCodeSpiritRedisCache(context.Configuration);
    }
}
