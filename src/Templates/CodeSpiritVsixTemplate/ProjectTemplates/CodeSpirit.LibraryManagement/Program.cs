using CodeSpirit.Core.Attributes;
using CodeSpirit.Infrastructure.AutoConfiguration;
using CodeSpirit.Infrastructure.Logging;

[CodeSpiritApplication]
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseCodeSpiritSerilog(builder.Configuration);

        builder.AddCodeSpirit();

        var app = builder.Build();

        app.UseCodeSpirit();

        app.Run();
    }
}