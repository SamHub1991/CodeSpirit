using CodeSpirit.Core.Attributes;

[CodeSpiritApplication]
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddCodeSpirit();
        var app = builder.Build();
        app.UseCodeSpirit();
        app.Run();
    }
}
