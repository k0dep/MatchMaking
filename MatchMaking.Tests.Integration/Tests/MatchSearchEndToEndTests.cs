using System.Net;
using System.Text;
using Confluent.Kafka;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace MatchMaking.IntegrationTests;

public sealed class MatchSearch_EndToEnd_Tests
    : IClassFixture<InfrastructureFixture>, IAsyncLifetime
{
    private readonly InfrastructureFixture _infra;
    private TestWebApplicationFactory _factory = null!;
    private HostApplicationFactory _backgroundFactory = null!;
    private HttpClient _client = null!;

    public MatchSearch_EndToEnd_Tests(InfrastructureFixture infra) => _infra = infra;

    public Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory(_infra.Redis, _infra.KafkaBootstrapServers);
        _backgroundFactory = new HostApplicationFactory(_infra.Redis, _infra.KafkaBootstrapServers);
        _client = _factory.CreateClient();
        _backgroundFactory.StartHostAsync().Wait();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        _ = _backgroundFactory.StopHostAsync();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Full_Pipeline_Should_Produce_Completion_And_Persist_To_Redis()
    {
        const string userId = "e2e-user-1";
        const string userId2 = "e2e-user-2";
        const string userId3 = "e2e-user-3";

        // call the API (raw JSON)
        var body = $$"""{ "userId": "{{userId}}" }""";
        var apiResp = await _client.PostAsync(
            "/match-search",
            new StringContent(body, Encoding.UTF8, "application/json"));
        apiResp.Should().HaveStatusCode(HttpStatusCode.Created);
        var infoResp = await _client.GetAsync($"/match-info?userId={userId}");
        infoResp.Should().HaveStatusCode(HttpStatusCode.NotFound);

        // 2nd shot - still no match
        body = $$"""{ "userId": "{{userId2}}" }""";
        apiResp = await _client.PostAsync(
            "/match-search",
            new StringContent(body, Encoding.UTF8, "application/json"));
        apiResp.Should().HaveStatusCode(HttpStatusCode.Created);
        
        infoResp = await _client.GetAsync($"/match-info?userId={userId}");
        infoResp.Should().HaveStatusCode(HttpStatusCode.NotFound);
        
        // 3rd shot - match found
        await Task.Delay(500);
        body = $$"""{ "userId": "{{userId3}}" }""";
        apiResp = await _client.PostAsync(
            "/match-search",
            new StringContent(body, Encoding.UTF8, "application/json"));
        
        apiResp.Should().HaveStatusCode(HttpStatusCode.Created);

        for (int i = 0; i < 30; i++)
        {
            infoResp = await _client.GetAsync($"/match-info?userId={userId}");
            if (infoResp.IsSuccessStatusCode)
            {
                break;
            }
            await Task.Delay(100);
        }
        
        infoResp.Should().HaveStatusCode(HttpStatusCode.OK);

        var infoPayload = JObject.Parse(await infoResp.Content.ReadAsStringAsync());
        infoPayload.Should().ContainKey("matchId");
        infoPayload.Should().ContainKey("userIds");
        infoPayload["userIds"]!.Select(x => x.Value<string>()).Should().BeEquivalentTo(userId, userId2, userId3);
    }
    
    [Fact]
    public async Task The_Same_User_Cannot_Form_A_Match()
    {
        const string userId = "e2e-the-same-user";

        // call the API (raw JSON)
        var body = $$"""{ "userId": "{{userId}}" }""";
        
        var apiResp = await _client.PostAsync(
            "/match-search",
            new StringContent(body, Encoding.UTF8, "application/json"));
        apiResp.Should().HaveStatusCode(HttpStatusCode.Created);
        
        await Task.Delay(500);
        
        apiResp = await _client.PostAsync(
            "/match-search",
            new StringContent(body, Encoding.UTF8, "application/json"));
        apiResp.Should().HaveStatusCode(HttpStatusCode.Created);
        
        await Task.Delay(500);
        
        apiResp = await _client.PostAsync(
            "/match-search",
            new StringContent(body, Encoding.UTF8, "application/json"));
        apiResp.Should().HaveStatusCode(HttpStatusCode.Created);
        
        HttpResponseMessage? infoResp = null;
        
        for (int i = 0; i < 30; i++)
        {
            infoResp = await _client.GetAsync($"/match-info?userId={userId}");
            if (infoResp.IsSuccessStatusCode)
            {
                break;
            }
            await Task.Delay(100);
        }
        
        infoResp.Should().HaveStatusCode(HttpStatusCode.NotFound);
    }
}