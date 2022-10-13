using AsyncUnitTestingPres;
using NSubstitute;

namespace Tests;

public class SyncItemsTest
{
    [Fact]
    public async Task SyncsNoItems()
    {
        var httpClient = Substitute.For<IHttpClient>();
        var items = new Dictionary<string, byte[]>();

        await Program.SyncItems(httpClient, items, CancellationToken.None);
    }
}