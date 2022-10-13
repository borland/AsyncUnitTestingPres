using System.Text;
using System.Text.Json;
using AsyncUnitTestingPres;
using FluentAssertions;

namespace Tests;

public class SyncItemsTest
{
    record struct LoginRequest(string username, string password);
    
    [Fact]
    public async Task SyncsTwoItems()
    {
        var httpClient = new MockHttpClient();
        var cancellationToken = CancellationToken.None;

        var localItems = new Dictionary<string, byte[]>
        {
            ["item1.txt"] = Encoding.ASCII.GetBytes("Hello"),
            ["item2.txt"] = Encoding.ASCII.GetBytes("World"),
        };

        httpClient.Enqueue(async (r) =>
        {
            r.Method.Should().Be(HttpMethod.Post);
            r.RequestUri.ToString()!.Should().Be("/api/session");
            var body = await r.Content.ReadAsStringAsync();
            
            var loginRequest = JsonSerializer.Deserialize<LoginRequest>(body);
            loginRequest.Should().Be(new LoginRequest("Orion", "secret"));
            return new HttpResponseMessage();
        });
        
        await Program.SyncItems(httpClient, localItems, cancellationToken);

        httpClient.RequestGenerators.Should().BeEmpty();
    }
}

public class MockHttpClient : IHttpClient
{
    public Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>> RequestGenerators {get;} = new();

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (RequestGenerators.Count == 0)
        {
            throw new InvalidOperationException($"Test failure; Received {request.Method} {request.RequestUri} but there was not a canned response for it");
        }
        
        var gen = RequestGenerators.Dequeue();
        var response = await gen(request);

        return response;
    }

    public void Enqueue(Func<HttpRequestMessage, Task<HttpResponseMessage>> generator)
        => RequestGenerators.Enqueue(generator);
}