using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace CodeSpirit.Infrastructure.Messaging;

[ConditionalOnClass("RabbitMQ.Client.IConnection")]
public class MessageBusModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddCodeSpiritMessageBus(context.Configuration);
    }

    public override void Configure(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<IEventBus>();
    }
}
