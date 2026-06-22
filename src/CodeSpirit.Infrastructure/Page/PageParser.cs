using System.Reflection;
using System.Text.RegularExpressions;
using CodeSpirit.Core;
using CodeSpirit.Core.Mvvm;
using CodeSpirit.Core.Page;

namespace CodeSpirit.Infrastructure.Page;

public record PageDescriptor(string? Route, Type ViewModelType, string? Layout, string? Title);

public class PageParser
{
    private static readonly Regex DirectiveRegex = new(
        @"@Page\s+([^*]*?)\*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PageDescriptor? ParseFromViewModelType(Type viewModelType)
    {
        var attr = viewModelType.GetCustomAttribute<PageDirectiveAttribute>();
        if (attr is null)
            return null;

        return new PageDescriptor(
            ViewModel.GetRoute(viewModelType),
            viewModelType,
            attr.Layout,
            attr.Title);
    }

    public PageDescriptor? ParseFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var content = File.ReadAllText(filePath);
        var match = DirectiveRegex.Match(content);
        if (!match.Success)
            return null;

        var directiveText = match.Groups[1].Value.Trim();
        var properties = ParseDirectiveProperties(directiveText);

        var route = properties.GetValueOrDefault("Route");
        var title = properties.GetValueOrDefault("Title");
        var layout = properties.GetValueOrDefault("Layout");

        return new PageDescriptor(route, typeof(object), layout, title);
    }

    public IEnumerable<PageDescriptor> DiscoverPages(params Assembly[] assemblies)
    {
        var scanAssemblies = assemblies.Length > 0 ? assemblies : Assemblies.CodeSpirit;

        var result = new List<PageDescriptor>();

        foreach (var asm in scanAssemblies)
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(ViewModel)))
                    continue;

                var descriptor = ParseFromViewModelType(type);
                if (descriptor is not null)
                    result.Add(descriptor);
            }
        }

        return result;
    }

    private static Dictionary<string, string> ParseDirectiveProperties(string directiveText)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pairs = directiveText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0 || eq >= pair.Length - 1)
                continue;

            var key = pair[..eq].Trim();
            var value = pair[(eq + 1)..].Trim('"', '\'');
            result[key] = value;
        }

        return result;
    }
}
