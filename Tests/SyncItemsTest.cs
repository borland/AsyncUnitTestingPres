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
        
        var ctx = await httpClient.ExpectRequest(HttpMethod.Post, "/api/items?name=item1.txt");
        var s = await ctx.Request.Content!.ReadAsStringAsync();
        s.Should().Be("Hello");
        ctx.Response.SetResult(new HttpResponseMessage());
        
        ctx = await httpClient.ExpectRequest(HttpMethod.Post, "/api/items?name=item2.txt");
        s = await ctx.Request.Content!.ReadAsStringAsync();
        s.Should().Be("World");
        ctx.Response.SetResult(new HttpResponseMessage());

        await outerSyncTask;
    }
}

public static class MockHttpClientExtensions
{
    public static async Task<MockRequestContext> ExpectRequest(this MockHttpClient httpClient, HttpMethod method, string url)
    {
        var ctx = await httpClient.NextRequest();
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
    private readonly Queue<MockRequestContext> _requests = new();

    // Approximation of an AutoResetEvent using Tasks
    private TaskCompletionSource<bool> _requestEnqueued = new();
    
    public async Task<MockRequestContext> NextRequest()
    {
        MockRequestContext? ctx;
        do
        {
            lock (this)
            {
                // if the SUT called SendAsync first, there'll be a request context sitting waiting for us that we can just use
                _requests.TryDequeue(out ctx);
            }

            if (ctx == null)
            {
                // else we have to wait for the SUT to call SendAsync
                await _requestEnqueued.Task;
                lock (this)
                {
                    _requestEnqueued = new();
                }
            }

        } while (ctx == null);
        
        return ctx;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // the SUT has made a request. Trigger the stalled unit test which should be waiting on NextRequest
        var responder = new TaskCompletionSource<HttpResponseMessage>();
        
        lock (this)
        {
            _requests.Enqueue(new MockRequestContext(request, responder));
        }
        _requestEnqueued.TrySetResult(true);
        
        // the unit test will now proceed. Eventually it should call SetResult on responder, in which case we can unblock the SUT
        var response = await responder.Task;

        return response;
    }
}

/* Flows:
1: Unit test goes first:
 - Test calls NextRequest
 - Test tries to dequeue a CTX, there isn't one
 - Test stalls on _requestEnqueued
 - SUT eventually calls SendAsync
 - SUT enqueues a new CTX and sets _requestEnqueued
 - SUT stalls on ctx.responder
 - Test resumes, does stuff (assertions etc)
 - Test sets ctx.responder and proceeds freely to the next part
 - SUT resumes, handles the response and proceeds freely to the next part
 
2: SUT goes first:
 - SUT calls SendAsync
 - SUT enqueues a new CTX and sets _requestEnqueued (nobody listening so who cares)
 - SUT stalls on ctx.responder
 - Test calls NextRequest
 - Test tries to dequeue a CTX, gets one
 - Test does stuff (assertions etc)
 - Test sets ctx.responder and proceeds freely to the next part
 - SUT resumes, handles the response and proceeds freely to the next part
*/

