namespace CodeSpirit.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class EndpointAttribute : Attribute
{
    public string Route { get; }

    public EndpointAttribute(string route)
    {
        Route = route;
    }
}
