using System.Net;
using System.Text.Json;

namespace CodeSpirit.Core;

/// <summary>
/// Built-in HTTP client for making requests.
/// Simple, async, typed deserialization.
///
/// Usage:
///   var users = await http.Get&lt;List&lt;User&gt;&gt;("https://api.example.com/users");
///   var result = await http.Post&lt;OrderResult&gt;("https://api.example.com/orders", order);
/// </summary>
public interface IHttp
{
    Task<T?> Get<T>(string url, CancellationToken ct = default);
    Task<T?> Post<T>(string url, object? body = null, CancellationToken ct = default);
    Task<T?> Put<T>(string url, object? body = null, CancellationToken ct = default);
    Task<T?> Delete<T>(string url, CancellationToken ct = default);
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);
}

/// <summary>
/// Default implementation using HttpClient.
/// </summary>
public class CodeSpiritHttp : IHttp
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public CodeSpiritHttp(HttpClient client) => _client = client;

    public async Task<T?> Get<T>(string url, CancellationToken ct = default)
    {
        var response = await _client.GetAsync(url, ct);
        return await Deserialize<T>(response, ct);
    }

    public async Task<T?> Post<T>(string url, object? body = null, CancellationToken ct = default)
    {
        var content = Serialize(body);
        var response = await _client.PostAsync(url, content, ct);
        return await Deserialize<T>(response, ct);
    }

    public async Task<T?> Put<T>(string url, object? body = null, CancellationToken ct = default)
    {
        var content = Serialize(body);
        var response = await _client.PutAsync(url, content, ct);
        return await Deserialize<T>(response, ct);
    }

    public async Task<T?> Delete<T>(string url, CancellationToken ct = default)
    {
        var response = await _client.DeleteAsync(url, ct);
        return await Deserialize<T>(response, ct);
    }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
        => _client.SendAsync(request, ct);

    private static StringContent? Serialize(object? body)
    {
        if (body is null) return null;
        var json = JsonSerializer.Serialize(body, JsonOptions);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response, CancellationToken ct)
    {
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct);
    }
}
