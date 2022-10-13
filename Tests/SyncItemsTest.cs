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

        var ctx1 = await httpClient.ExpectJsonRequest(HttpMethod.Post, "/api/session", new LoginRequest("Orion", "secret"));
        ctx1.Response.SetResult(new HttpResponseMessage());
        
        (await httpClient.ExpectRequest(HttpMethod.Get, "/api/items"))
            .Response.SetResult(new HttpResponseMessage{ Content = new StringContent(JsonSerializer.Serialize(new string[0]))});
        
        var ctx = await httpClient.ExpectRequest(HttpMethod.Post, "/api/files?name=file1.txt");
        var s = await ctx.Request.Content.ReadAsStringAsync();
        s.Should().Be("Hello");
        ctx.Response.SetResult(new HttpResponseMessage());
        
        ctx = await httpClient.ExpectRequest(HttpMethod.Post, "/api/files?name=file2.txt");
        s = await ctx.Request.Content.ReadAsStringAsync();
        s.Should().Be("World");
        ctx.Response.SetResult(new HttpResponseMessage());

        await outerSyncTask;

        // httpClient.RequestGenerators.Should().BeEmpty();
    }
}

public static class MockHttpClientExtensions
{
    public static async Task<MockRequestContext> ExpectRequest(this MockHttpClient httpClient, HttpMethod method, string url)
    {
        var ctx = httpClient.CurrentRequestContext;
        ctx.Should().NotBeNull();
        
        ctx.Request.Should().NotBeNull();
        ctx.Request.Method.Should().Be(method);
        ctx.Request.RequestUri.ToString().Should().Be(url);

        return ctx;
    }
    
    public static async Task<MockRequestContext> ExpectJsonRequest<TRequestBody>(this MockHttpClient httpClient, HttpMethod method, string url, TRequestBody? body = default)
    {
        var ctx = await ExpectRequest(httpClient, method, url);

        if (body != null)
        {
            var requestBodyString = await ctx.Request.Content.ReadAsStringAsync();
            var requestBodyObj = JsonSerializer.Deserialize<TRequestBody>(requestBodyString);
            requestBodyObj.Should().BeEquivalentTo(body);
        }

        return ctx;
    }
}

public record MockRequestContext(HttpRequestMessage Request, TaskCompletionSource<HttpResponseMessage> Response); 

public class MockHttpClient : IHttpClient
{
    private TaskCompletionSource<HttpResponseMessage> _nextResponder = new();
    
    public MockRequestContext? CurrentRequestContext { get; private set; }

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        TaskCompletionSource<HttpResponseMessage> responder;
        lock (this)
        {
            responder = _nextResponder;

            CurrentRequestContext = new(request, responder);

            _nextResponder = new();
        }

        return responder.Task;
    }
}