// See https://aka.ms/new-console-template for more information

using System.Text;

namespace AsyncUnitTestingPres;

public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("Hello, World!");

        var httpClient = new RealHttpClient(new HttpClient {  BaseAddress = new Uri("http://someserver") });
        
        await SyncItems(httpClient, Items, CancellationToken.None);
    }
    
    static async Task SyncItems(IHttpClient httpClient, Dictionary<string, byte[]> items, CancellationToken cancellationToken)
    {
        await httpClient.PostJsonAsync("/api/session", new { username = "Orion", password = "secret" }, cancellationToken);

        var itemSet = new HashSet<string>
            (await httpClient.GetJsonAsync<string[]>("/api/items", cancellationToken));
        
        foreach (var (itemName, itemValue) in items)
        {
            if(itemSet.Contains(itemName)) continue;
            
            var req = new HttpRequestMessage(HttpMethod.Post, new Uri($"/api/items?name={itemName}"));
            req.Content = new ByteArrayContent(itemValue);

            var response = await httpClient.SendAsync(req, cancellationToken);
            if(!response.IsSuccessStatusCode) throw new InvalidHttpResponseException($"Unexpected HTTP Response {response.StatusCode}");
        }
    }
    
    private static Dictionary<string, byte[]> Items = new()
    {
        ["Item1.txt"] = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dogs"),
        ["Item2.txt"] = Encoding.ASCII.GetBytes("Nine boxing wizards jump quickly"),
        ["Item3.cer"] = Convert.FromBase64String("MIIDzTCCArWgAwIBAgIQa/ZKcCidG7tGaBEaN24J9TANBgkqhkiG9w0BAQsFADBnMSswKQYDVQQLDCJDcmVhdGVkIGJ5IGh0dHA6Ly93d3cuZmlkZGxlcjIuY29tMRUwEwYDVQQKDAxET19OT1RfVFJVU1QxITAfBgNVBAMMGERPX05PVF9UUlVTVF9GaWRkbGVyUm9vdDAeFw0yMjA3MjExNTUxMjJaFw0yMzA3MjExNTUxMjJaMFsxKzApBgNVBAsMIkNyZWF0ZWQgYnkgaHR0cDovL3d3dy5maWRkbGVyMi5jb20xFTATBgNVBAoMDERPX05PVF9UUlVTVDEVMBMGA1UEAwwMKi5nb29nbGUuY29tMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAyi+6+KJQ4d1cjNjkmxqQRiNIphKJluxlFaJ1SQM4E0QpHUxoKFnAz6FXKPPOfZOjgajstcNZkr9UlLsb5m2UI0eII2uo+Pk7m+U6zKBAbpa4CYN8wot0iGUpLU92Y2Q0YP3Q3U1dQLTMqV//wUHSwW2qzIFv6eIi7bZ/NvPFZPt5nbQKVrJCi9nbnXwqVOAGtchNrzsup9LviJWzKMI3J3quhLZx80CginjleY0DcIT8zQyeTH2VnrS4RJ659xo71yoiZzOf0ji1a5+R5NUldz+Ygj3/Mv7uw9P7UG+LLKbypxlSsm8XiovfxPrEKvt/QGSLjQg7H46X4T7ilnZOxQIDAQABo4GAMH4wDgYDVR0PAQH/BAQDAgSwMBMGA1UdJQQMMAoGCCsGAQUFBwMBMBcGA1UdEQQQMA6CDCouZ29vZ2xlLmNvbTAfBgNVHSMEGDAWgBS9QaubNcvW/GU6RpxugiXievlSNzAdBgNVHQ4EFgQUCLnQB10Wobxpsbjn3yfgdJDOFMQwDQYJKoZIhvcNAQELBQADggEBAGiRlrGuLugKYKRv4SWmEFn0pMukT5Y38P1pxagc/1rVokBIkkA5UFJVk5pA/qOlS+1enJkP2YBFqtBalf59CQ8VY2kANiCzmllqQw/qpwKRJZYMPYc0oqydzpqzZjKa72021yA439gnKNWS6SSpFqhh9FpjzViMfFevxT9emcRNjKQgY5xcDLWr4//3arTHurNBAtDUDVWrBR4eqZddRlSsiD6mJDj7cr2yPZSrnsBqvEn3OxSU22035w7oyPCcEwnYB3225XqO1JPEbOK1sE1H5wgS2D2Rygc79mVnXSz+Zc8WrvKuNkvpH9asdP78+D4GnwOY9jsjG6ReLYbx/lk="),
    };
}