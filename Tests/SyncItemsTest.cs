using System.Net;
using System.Text.Json;
using AsyncUnitTestingPres;
using FluentAssertions;

namespace Tests;

public class SyncItemsTest
{
    [Fact]
    public async Task SyncsNoItems()
    {
        var httpClient = new MockHttpClient();
        var cancellationToken = CancellationToken.None;

        httpClient.RespondWith(HttpMethod.Post, "/api/session", HttpStatusCode.Created);
        httpClient.RespondWithJson(HttpMethod.Get, "/api/items", HttpStatusCode.OK, new[]{"item1.txt", "item2.txt"});
        httpClient.RespondWith(HttpMethod.Post, "/api/items?name=item1.txt", HttpStatusCode.OK);
        httpClient.RespondWith(HttpMethod.Post, "/api/items?name=item2.txt", HttpStatusCode.OK);
        
        await Program.SyncItems(httpClient, new Dictionary<string, byte[]>(), cancellationToken);

        httpClient.ReceivedRequests.Count.Should().Be(4);
    }
}

public record struct ReceivedRequest(HttpMethod Method, string Url, HttpRequestMessage Request);

public class MockHttpClient : IHttpClient
{
    record struct RequestKey(HttpMethod Method, string Url);

    private Dictionary<RequestKey, HttpResponseMessage> CannedResponses = new();
    
    public List<ReceivedRequest> ReceivedRequests {get;} = new();

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestKey = new RequestKey(request.Method, request.RequestUri!.ToString());
        ReceivedRequests.Add(new(requestKey.Method, requestKey.Url, request));
        
        if (CannedResponses.TryGetValue(requestKey, out var responseMessage))
            return Task.FromResult(responseMessage);

        return Task.FromException<HttpResponseMessage>(new InvalidOperationException($"Test failure; Received {requestKey} but there was not a canned response for it"));
    }
    
    public void RespondWith(HttpMethod method, string url, HttpResponseMessage response)
        => CannedResponses[new(method, url)] = response;
    
    public void RespondWith(HttpMethod method, string url, HttpStatusCode statusCode)
        => RespondWith(method, url, new HttpResponseMessage(statusCode));
    
    public void RespondWithJson(HttpMethod method, string url, HttpStatusCode statusCode, object responseBody)
        => RespondWith(method, url, new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(JsonSerializer.Serialize(responseBody))
        });
}