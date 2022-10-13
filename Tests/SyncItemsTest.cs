using System.Net;
using AsyncUnitTestingPres;
using NSubstitute;

namespace Tests;

public class SyncItemsTest
{
    [Fact]
    public async Task SyncsNoItems()
    {
        var httpClient = Substitute.For<IHttpClient>();
        var cancellationToken = CancellationToken.None;

        var r1 = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("/api/session", UriKind.RelativeOrAbsolute),
        };
        httpClient.SendAsync(r1, cancellationToken).Returns(new HttpResponseMessage(HttpStatusCode.Created));

        await Program.SyncItems(httpClient, new Dictionary<string, byte[]>(), cancellationToken);
    }
}