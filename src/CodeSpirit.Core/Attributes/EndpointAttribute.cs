namespace CodeSpirit.Core.Attributes;

/// <summary>
/// Maps a method as an HTTP endpoint with automatic routing and parameter binding.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// [Endpoint("/api/health")]
/// public IResult Health() => Results.Ok(new { status = "UP" });
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public class EndpointAttribute : Attribute
{
    /// <summary>
    /// URL route pattern for the endpoint.
    /// </summary>
    public string Route { get; }

    public EndpointAttribute(string route)
    {
        Route = route;
    }
}
