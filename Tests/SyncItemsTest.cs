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
        
        var outerSyncTask = Program.SyncItems(httpClient, localItems, cancellationToken); // NOTE NO AWAIT

        var r = httpClient.CurrentRequest?.Request;
        r.Should().NotBeNull();
        r.Method.Should().Be(HttpMethod.Post);
        r.RequestUri.ToString()!.Should().Be("/api/session");
        var body = await r.Content.ReadAsStringAsync();
            
        var loginRequest = JsonSerializer.Deserialize<LoginRequest>(body);
        loginRequest.Should().Be(new LoginRequest("Orion", "secret"));
        httpClient.CurrentRequest.Response.SetResult(new HttpResponseMessage());

        await outerSyncTask;

        // httpClient.RequestGenerators.Should().BeEmpty();
    }
}

public record MockRequestContext(HttpRequestMessage Request, TaskCompletionSource<HttpResponseMessage> Response); 

public class MockHttpClient : IHttpClient
{
    private TaskCompletionSource<HttpResponseMessage> _nextResponder = new();
    
    public MockRequestContext? CurrentRequest { get; private set; }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        TaskCompletionSource<HttpResponseMessage> responder;
        lock (this)
        {
            responder = _nextResponder;

            CurrentRequest = new(request, responder);

            _nextResponder = new();
        }

        return responder.Task;
    }
}