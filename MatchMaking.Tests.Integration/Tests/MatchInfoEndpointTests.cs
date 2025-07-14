using System.Net;
using FluentAssertions;

namespace MatchMaking.IntegrationTests;

public sealed class MatchInfoEndpointTests
    : IClassFixture<InfrastructureFixture>, IAsyncLifetime
{
    private readonly InfrastructureFixture _infra;
    private TestWebApplicationFactory _factory = default!;
    private HttpClient _client = default!;

    public MatchInfoEndpointTests(InfrastructureFixture infra) => _infra = infra;

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
    public async Task Get_MatchInfo_For_Missing_Record_Should_Return_404()
    {
        var response = await _client.GetAsync("/match-info?userId=u-missing");
        response.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Get_MatchInfo_Invalid_UserId_Should_Return_400(string? userId)
    {
        var response = await _client.GetAsync($"/match-info?userId={userId}");
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);   
    }
}