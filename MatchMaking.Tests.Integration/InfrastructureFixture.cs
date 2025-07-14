using System.Diagnostics;
using StackExchange.Redis;
using Testcontainers.Kafka;
using Testcontainers.Redis;

namespace MatchMaking.IntegrationTests;

/// <summary>
/// Boots real Redis + Kafka instances and exposes the connection
/// information to the test-cases.
/// </summary>
public sealed class InfrastructureFixture : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly KafkaContainer _kafkaContainer;
    
    public IConnectionMultiplexer Redis { get; private set; } = default!;
    public string KafkaBootstrapServers => _kafkaContainer.GetBootstrapAddress();

    public InfrastructureFixture()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7.2-alpine")
            .Build();

        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.6.0")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redisContainer.StartAsync();
        await _kafkaContainer.StartAsync();

        var redisEndpoint = $"{_redisContainer.Hostname}:{_redisContainer.GetMappedPublicPort(6379)}";
        Redis = await ConnectionMultiplexer.ConnectAsync(redisEndpoint);
    }
    
    public async Task DisposeAsync()
    {
        await Redis.CloseAsync();
        await _redisContainer.DisposeAsync();
        await _kafkaContainer.DisposeAsync();
    }
}