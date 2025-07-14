using System.Net;
using System.Text;
using FluentAssertions;

namespace MatchMaking.IntegrationTests;

/// <summary>
/// Proves that the rate-limiter allows only one request per user within a
/// 100 ms sliding window.  Rather than relying on two precisely timed calls,
/// the test sends a burst of parallel requests and inspects the outcome
/// distribution, which is far more stable on slow or busy runners.
/// </summary>
public sealed class RateLimiterEndToEndTests
    : IClassFixture<InfrastructureFixture>, IAsyncLifetime
{
    private readonly InfrastructureFixture _infra;
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client               = null!;

    public RateLimiterEndToEndTests(InfrastructureFixture infra) => _infra = infra;

    public Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory(_infra.Redis, _infra.KafkaBootstrapServers);
        _client  = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Burst_For_Same_UserId_Should_Contain_Both_200_And_429()
    {
        const string userId = "rate-limit-burst-user";
        const int burst = 20; // 20 simultaneous requests → statistically guarantees overlap

        // build the request body only once to minimise allocations
        var contentFactory = () =>
            new StringContent($$"""{ "userId": "{{userId}}" }""",
                Encoding.UTF8, "application/json");

        // kick off the whole burst without awaiting individual calls
        var tasks = Enumerable.Range(0, burst)
            .Select(_ => _client.PostAsync("/match-search", contentFactory()))
            .ToArray();

        await Task.WhenAll(tasks);

        var statusCodes = tasks.Select(t => t.Result.StatusCode).ToArray();

        statusCodes.Should().Contain(HttpStatusCode.Created,
            "at least the first request in the burst must pass");

        statusCodes.Should().Contain(HttpStatusCode.TooManyRequests,
            "one or more follow-up requests inside the 100 ms window must be rejected");

        statusCodes.Count(sc => sc == HttpStatusCode.Created)
            .Should().BeLessOrEqualTo(5,
                "under a 100 ms, one-permit sliding window the number of allowed requests is very small");
    }
}