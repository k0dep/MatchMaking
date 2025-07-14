using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using MatchMaking.Contracts.Dtos;
using MatchMaking.Kafka;
using MatchMaking.Worker.Workers;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

if (await CheckHealthAsync(args, builder))
{
    return;
}

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

// Kafka consumer / producer
builder.Services.AddSingleton<IConsumer<string, MatchSearchRequestMessage>>(sp =>
{
    var cfg = builder.Configuration;
    var consumerConfig = new ConsumerConfig
    {
        BootstrapServers = cfg["Kafka:BootstrapServers"],
        GroupId = cfg["Kafka:GroupId"],
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest
    };
    return new ConsumerBuilder<string, MatchSearchRequestMessage>(consumerConfig)
        .SetValueDeserializer(new KafkaJsonDeserializer<MatchSearchRequestMessage>().AsSyncOverAsync())
        .Build();
});

builder.Services.AddSingleton<IProducer<string, MatchCompleteMessage>>(sp =>
{
    var cfg = builder.Configuration["Kafka:BootstrapServers"];
    var producerConfig = new ProducerConfig { BootstrapServers = cfg };
    return new ProducerBuilder<string, MatchCompleteMessage>(producerConfig)
        .SetValueSerializer(new KafkaJsonSerializer<MatchCompleteMessage>())
        .Build();
});

builder.Services.AddHostedService<MatchMakingWorker>();

await builder.Build().RunAsync();

async Task<bool> CheckHealthAsync(string[] strings, HostApplicationBuilder hostApplicationBuilder)
{
    if (strings is not ["healthcheck"])
    {
        return false;
    }
    
    try
    {
        // ─ Redis
        var redisConn = hostApplicationBuilder.Configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redisConn))
            throw new InvalidOperationException("Redis connection string missing");

        await using (var mux = await ConnectionMultiplexer.ConnectAsync(redisConn))
        {
            _ = await mux.GetDatabase().PingAsync(); // throws if not reachable
        }

        // ─ Kafka
        var kafkaBootstrap = hostApplicationBuilder.Configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(kafkaBootstrap))
            throw new InvalidOperationException("Kafka bootstrap servers missing");

        var adminCfg = new AdminClientConfig { BootstrapServers = kafkaBootstrap };
        using (var admin = new AdminClientBuilder(adminCfg).Build())
        {
            // request metadata; will throw if broker unreachable
            admin.GetMetadata(TimeSpan.FromSeconds(5));
        }

        Console.WriteLine("Healthy");
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Unhealthy: {ex.Message}");
        Environment.Exit(1);
    }
    return true;
}

namespace MatchMaking.Worker
{
    public partial class Program;
}