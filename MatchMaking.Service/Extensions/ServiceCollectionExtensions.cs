using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using MatchMaking.Contracts.Dtos;
using MatchMaking.Kafka;
using Microsoft.AspNetCore.Http.Features;

namespace MatchMaking.Service.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConsumersAndProducers(this IServiceCollection services, IConfiguration cfg)
    {
        // Kafka consumer for "matchmaking.complete"
        services.AddSingleton<IConsumer<string, MatchCompleteMessage>>(sp =>
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = cfg["Kafka:BootstrapServers"],
                GroupId = cfg["Kafka:GroupId"],
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            return new ConsumerBuilder<string, MatchCompleteMessage>(consumerConfig)
                .SetValueDeserializer(new KafkaJsonDeserializer<MatchCompleteMessage>().AsSyncOverAsync())
                .Build();
        });

        // Kafka producer for "matchmaking.request"
        services.AddSingleton<IProducer<string, MatchSearchRequestMessage>>(sp =>
        {
            var producerConfig = new ProducerConfig { BootstrapServers =  cfg["Kafka:BootstrapServers"] };
            return new ProducerBuilder<string, MatchSearchRequestMessage>(producerConfig)
                .SetValueSerializer(new KafkaJsonSerializer<MatchSearchRequestMessage>().AsSyncOverAsync())
                .Build();
        });
        return services;
    }

    public static IServiceCollection AddApplicationRateLimiter(this IServiceCollection services)
    {
        // Rate-limiter – 1 request / 100 ms per user
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy("match-search", ctx =>
            {
                ctx.Request.EnableBuffering();
                var userId = string.Empty;
        
                var syncIoFeature = ctx.Features.Get<IHttpBodyControlFeature>();
                if (syncIoFeature != null)
                {
                    syncIoFeature.AllowSynchronousIO = true;
                }

                ctx.Request.EnableBuffering();
                var buffer = new byte[Convert.ToInt32(ctx.Request.ContentLength)];
                ctx.Request.Body.ReadExactly(buffer);
                var requestContent = Encoding.UTF8.GetString(buffer);
                ctx.Request.Body.Position = 0;

                try
                {
                    using var doc = JsonDocument.Parse(requestContent);
                    if (doc.RootElement.TryGetProperty("userId", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.String)
                    {
                        userId = idProp.GetString() ?? string.Empty;
                    }
                }
                catch (JsonException)
                {
                }

                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: userId.Trim(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 1,
                        Window = TimeSpan.FromMilliseconds(100),
                        SegmentsPerWindow = 1
                    });
            });
        });

        return services;
    }
}