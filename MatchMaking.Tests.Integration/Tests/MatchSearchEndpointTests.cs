using System.Net;
using System.Text;
using Confluent.Kafka;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace MatchMaking.IntegrationTests;

public sealed class MatchSearchEndpointTests
    : IClassFixture<InfrastructureFixture>, IAsyncLifetime
{
    private readonly InfrastructureFixture _infra;
    private TestWebApplicationFactory _factory = default!;
    private HttpClient _client = default!;

    public MatchSearchEndpointTests(InfrastructureFixture infra) => _infra = infra;

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
    public async Task Post_MatchSearch_Should_Produce_Correct_Kafka_Message()
    {
        const string userId = "user-happy-1";

        var jsonBody = $$"""
                         {
                           "userId": "{{userId}}"
                         }
                         """;

        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var apiResponse = await _client.PostAsync("/match-search", content);

        apiResponse.Should().HaveStatusCode(HttpStatusCode.Created);

        var consumerCfg = new ConsumerConfig
        {
            BootstrapServers = _infra.KafkaBootstrapServers,
            GroupId = $"verify-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerCfg)
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        consumer.Subscribe("matchmaking.request"); // topic used by the app

        var consumed = consumer.Consume(TimeSpan.FromSeconds(5));
        consumed.Should().NotBeNull("a message must be published");

        var json = JObject.Parse(consumed!.Message.Value);
        json.Should().ContainKey("UserId");
        json["UserId"]!.Value<string>().Should().Be(userId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Post_MatchSearch_With_Invalid_UserId_Should_Be_BadRequest(string? invalidUser)
    {
        var jsonBody = $$"""
                         {
                           "userId": {{(invalidUser is null ? "null" : $"\"{invalidUser}\"")}}
                         }
                         """;

        using var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        var apiResponse = await _client.PostAsync("/match-search", content);

        apiResponse.Should().HaveStatusCode(HttpStatusCode.BadRequest);
    }
}