using CodeSpirit.Core.Abstractions;
using CodeSpirit.Core.Attributes;

namespace CodeSpirit.Infrastructure.Logging;

[ConditionalOnClass("Serilog.LoggerConfiguration, Serilog")]
public class SerilogModule : CodeSpiritModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
