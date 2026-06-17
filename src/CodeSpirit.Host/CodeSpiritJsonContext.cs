using CodeSpirit.Infrastructure.AutoConfiguration;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace CodeSpirit.Host;

[JsonSerializable(typeof(CodeSpiritOptions))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
internal partial class CodeSpiritJsonContext : JsonSerializerContext
{
}
