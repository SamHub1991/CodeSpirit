using System.Text.Json;

namespace CodeSpirit.Core;

public static class CodeSpiritDefaults
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public const string ConfigurationPrefix = "CodeSpirit";
    public const string ProfileKey = "CodeSpirit:Profile";
    public const string ProfileDefault = "default";

    public const string CommandParamKey = "__command";
    public const string CommandAltParamKey = "command";

    public const string ContentTypeJson = "application/json";
    public const string ContentTypeJsonUtf8 = "application/json; charset=utf-8";

    public const string DefaultLayout = "Pages/Site.master";
    public const string PagesDirectory = "Pages";
    public const string ProductName = "CodeSpirit";
    public const string PoweredByHeader = "X-Powered-By";
}
