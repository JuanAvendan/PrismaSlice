using System.Net;
using PrismaSlice.IntegrationTests.Support;

namespace PrismaSlice.IntegrationTests.Hosting;

public class HostingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await factory.CreateClient().GetAsync("/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_OverTheLimit_Returns429WithRetryAfter()
    {
        var client = factory.CreateClientWithOwnIp();
        const int searchLimitPerMinute = 30;

        for (var i = 0; i < searchLimitPerMinute; i++)
        {
            var ok = await client.GetAsync("/api/tickets/search?q=impresora", Ct);
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var limited = await client.GetAsync("/api/tickets/search?q=impresora", Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.NotNull(limited.Headers.RetryAfter);
        Assert.Equal("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task RateLimit_IsPerClient()
    {
        var noisy = factory.CreateClientWithOwnIp();
        for (var i = 0; i < 31; i++)
            await noisy.GetAsync("/api/tickets/search?q=impresora", Ct);

        var other = await factory.CreateClientWithOwnIp().GetAsync("/api/tickets/search?q=impresora", Ct);

        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }
}
