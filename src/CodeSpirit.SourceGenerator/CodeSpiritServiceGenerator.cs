using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace CodeSpirit.SourceGenerator;

[Generator]
public class CodeSpiritServiceGenerator : IIncrementalGenerator
{
    private const string ServiceAttributeName = "CodeSpirit.Core.Attributes.ServiceAttribute";
    private const string RepositoryAttributeName = "CodeSpirit.Core.Attributes.RepositoryAttribute";
    private const string CommandAttributeName = "CodeSpirit.Core.Mvvm.CommandAttribute";

    private static readonly DiagnosticDescriptor AbstractServiceWarning = new(
        "CSP001",
        "Abstract class with [Service] is ignored",
        "Class '{0}' is abstract and will not be registered. Remove [Service] or make the class non-abstract.",
        "CodeSpirit",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NoPublicConstructorWarning = new(
        "CSP002",
        "[Service] class missing public constructor",
        "Class '{0}' has no accessible public constructor. CodeSpirit may fail to activate this service.",
        "CodeSpirit",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor CommandHasParametersError = new(
        "CSP003",
        "[Command] method must not have parameters",
        "Command '{0}' declares parameters. Command methods must be parameterless.",
        "CodeSpirit",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetServiceRegistrationInfo(ctx))
            .Where(static info => info is not null);

        var combined = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(combined, GenerateServiceRegistration);

        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0 && m.ParameterList.Parameters.Count > 0,
                transform: static (ctx, _) => GetCommandDiagnosticInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(methods.Collect(), GenerateCommandDiagnostics);
    }

    private static ServiceRegistrationInfo? GetServiceRegistrationInfo(GeneratorSyntaxContext ctx)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        var model = ctx.SemanticModel;
        var symbol = model.GetDeclaredSymbol(classDecl);

        if (symbol is not INamedTypeSymbol typeSymbol)
            return null;

        var serviceLifetime = ServiceLifetimeKind.Scoped;
        var hasService = false;
        string? interfaceName = null;
        var hasRepository = false;

        foreach (var attr in typeSymbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null) continue;

            var fullName = attrClass.ToDisplayString();

            if (fullName == ServiceAttributeName)
            {
                hasService = true;
                foreach (var named in attr.NamedArguments)
                {
                    if (named.Key == "Lifetime" && named.Value.Value is int lifetimeVal)
                    {
                        serviceLifetime = (ServiceLifetimeKind)lifetimeVal;
                    }
                    else if (named.Key == "ServiceType" && named.Value.Value is INamedTypeSymbol st)
                    {
                        interfaceName = st.ToDisplayString();
                    }
                }
            }
            else if (fullName == RepositoryAttributeName)
            {
                hasRepository = true;
            }
        }

        if (!hasService && !hasRepository)
            return null;

        return new ServiceRegistrationInfo
        {
            ClassName = typeSymbol.ToDisplayString(),
            InterfaceName = interfaceName
                ?? typeSymbol.AllInterfaces
                    .FirstOrDefault(i => !i.ToDisplayString().Contains("ICodeSpiritModule"))
                    ?.ToDisplayString()
                ?? typeSymbol.ToDisplayString(),
            Lifetime = serviceLifetime,
            IsRepository = hasRepository,
            IsAbstract = typeSymbol.IsAbstract,
            HasPublicConstructor = typeSymbol.Constructors.Any(c => c.DeclaredAccessibility == Accessibility.Public),
            Location = typeSymbol.Locations.FirstOrDefault()
        };
    }

    private static CommandDiagnosticInfo? GetCommandDiagnosticInfo(GeneratorSyntaxContext ctx)
    {
        var methodDecl = (MethodDeclarationSyntax)ctx.Node;
        var model = ctx.SemanticModel;
        var symbol = model.GetDeclaredSymbol(methodDecl);

        if (symbol is not IMethodSymbol methodSymbol || methodSymbol.Parameters.Length == 0)
            return null;

        var hasCommand = methodSymbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == CommandAttributeName);

        if (!hasCommand)
            return null;

        return new CommandDiagnosticInfo
        {
            MethodName = methodSymbol.Name,
            Location = methodSymbol.Locations.FirstOrDefault()
        };
    }

    private static void GenerateServiceRegistration(
        SourceProductionContext context,
        (Compilation Left, ImmutableArray<ServiceRegistrationInfo?> Right) source)
    {
        var allInfos = new List<ServiceRegistrationInfo>();
        var seen = new HashSet<string>();

        foreach (var info in source.Right)
        {
            if (info is null || !seen.Add(info.ClassName))
                continue;

            if (info.IsAbstract)
            {
                if (info.Location is not null)
                    context.ReportDiagnostic(Diagnostic.Create(AbstractServiceWarning, info.Location, info.ClassName));
                continue;
            }

            if (!info.HasPublicConstructor && info.Location is not null)
                context.ReportDiagnostic(Diagnostic.Create(NoPublicConstructorWarning, info.Location, info.ClassName));

            allInfos.Add(info);
        }

        if (allInfos.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.Configuration;");
        sb.AppendLine("using CodeSpirit.Core.Interfaces;");
        sb.AppendLine();
        sb.AppendLine("namespace CodeSpirit.AutoGenerated");
        sb.AppendLine("{");
        sb.AppendLine("    public sealed class CodeSpiritGeneratedRegistrar : ICodeSpiritGeneratedRegistrar");
        sb.AppendLine("    {");
        sb.AppendLine("        public void RegisterServices(IServiceCollection services, IConfiguration configuration)");
        sb.AppendLine("        {");

        foreach (var reg in allInfos)
        {
            var lifetimeStr = reg.IsRepository ? "Scoped" : reg.Lifetime switch
            {
                ServiceLifetimeKind.Singleton => "Singleton",
                ServiceLifetimeKind.Scoped => "Scoped",
                ServiceLifetimeKind.Transient => "Transient",
                _ => "Scoped"
            };

            sb.AppendLine($"            services.Add{lifetimeStr}<{reg.InterfaceName}, {reg.ClassName}>();");
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("CodeSpiritServiceRegistration.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void GenerateCommandDiagnostics(
        SourceProductionContext context,
        ImmutableArray<CommandDiagnosticInfo?> infos)
    {
        foreach (var info in infos)
        {
            if (info?.Location is not null)
                context.ReportDiagnostic(Diagnostic.Create(CommandHasParametersError, info.Location, info.MethodName));
        }
    }
}

internal sealed class ServiceRegistrationInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string? InterfaceName { get; set; }
    public ServiceLifetimeKind Lifetime { get; set; } = ServiceLifetimeKind.Scoped;
    public bool IsRepository { get; set; }
    public bool IsAbstract { get; set; }
    public bool HasPublicConstructor { get; set; }
    public Location? Location { get; set; }
}

internal sealed class CommandDiagnosticInfo
{
    public string MethodName { get; set; } = string.Empty;
    public Location? Location { get; set; }
}

internal enum ServiceLifetimeKind
{
    Singleton = 0,
    Scoped = 1,
    Transient = 2
}
