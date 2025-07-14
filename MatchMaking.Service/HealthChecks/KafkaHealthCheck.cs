using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MatchMaking.Service.HealthChecks;

public sealed class KafkaHealthCheck : IHealthCheck
{
    private readonly string _bootstrapServers;

    public KafkaHealthCheck(IConfiguration cfg) =>
        _bootstrapServers = cfg["Kafka:BootstrapServers"]!;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var admin = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = _bootstrapServers })
                .Build();

            admin.GetMetadata(TimeSpan.FromSeconds(5));
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka unavailable", ex));
        }
    }
}