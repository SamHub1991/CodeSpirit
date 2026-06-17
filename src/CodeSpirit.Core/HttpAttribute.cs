namespace CodeSpirit.Core;

/// <summary>
/// Marks a method as an HTTP request handler.
/// The method's return value is automatically used as the request context.
///
/// Usage:
///   [Http.Get("https://api.example.com/users")]
///   public async Task&lt;List&lt;User&gt;&gt; FetchUsers() { ... }
///
/// The framework provides built-in IHttpClient, but you can also
/// annotate methods to auto-declare remote API calls.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HttpAttribute : Attribute
{
    public string Method { get; }
    public string Url { get; }
    public string? Name { get; set; }
    public int TimeoutMs { get; set; } = 30000;
    public string? HeaderKey { get; set; }
    public string? HeaderValue { get; set; }

    protected HttpAttribute(string method, string url)
    {
        Method = method;
        Url = url;
    }

    public static HttpAttribute Get(string url) => new("GET", url);
    public static HttpAttribute Post(string url) => new("POST", url);
    public static HttpAttribute Put(string url) => new("PUT", url);
    public static HttpAttribute Delete(string url) => new("DELETE", url);
}

/// <summary>
/// Simplified attribute for GET requests.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HttpGetAttribute : HttpAttribute
{
    public HttpGetAttribute(string url) : base("GET", url) { }
}

/// <summary>
/// Simplified attribute for POST requests.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HttpPostAttribute : HttpAttribute
{
    public HttpPostAttribute(string url) : base("POST", url) { }
}
