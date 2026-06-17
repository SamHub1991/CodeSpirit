using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Infrastructure.Telemetry;

[ConditionalOnClass("OpenTelemetry.Sdk, OpenTelemetry")]
public class TelemetryModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddCodeSpiritTelemetry(context.Configuration);
    }
}
