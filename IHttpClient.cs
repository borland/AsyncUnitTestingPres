using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AsyncUnitTestingPres;

public interface IHttpClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}

public class RealHttpClient : IHttpClient
{
    readonly System.Net.Http.HttpClient _client;

    public RealHttpClient(System.Net.Http.HttpClient client)
        => _client = client;

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _client.SendAsync(request, cancellationToken);
}

public class InvalidHttpResponseException : Exception
{
    public InvalidHttpResponseException(string message) : base(message) 
    { }
}

public static class IHttpClientExtensions
{
    public static Task<TResponseBody> GetJsonAsync<TResponseBody>(this IHttpClient client, string url, CancellationToken cancellationToken)
        => SendJsonAsync<object, TResponseBody>(client, HttpMethod.Get, url, null, cancellationToken);
    
    public static Task PostJsonAsync<TRequestBody>(this IHttpClient client, string url, TRequestBody? body, CancellationToken cancellationToken)
        => SendJsonAsync<object, object>(client, HttpMethod.Post, url, body, cancellationToken);
    
    public static async Task<TResponseBody> SendJsonAsync<TRequestBody, TResponseBody>(
        this IHttpClient client, 
        HttpMethod method, 
        string url, 
        TRequestBody body, 
        CancellationToken cancellationToken)
    {
        var req = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(url),
        };
        if (body != null)
        {
            var jsonBody = JsonSerializer.Serialize(body);
            req.Content = new StringContent(jsonBody);
        }

        var response = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidHttpResponseException($"Unexpected HTTP Response {response.StatusCode}");

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponseBody>(responseText);
    } 
}

