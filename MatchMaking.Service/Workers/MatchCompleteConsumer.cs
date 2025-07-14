using Confluent.Kafka;
using MatchMaking.Contracts.Dtos;
using StackExchange.Redis;

namespace MatchMaking.Service.Workers;

public sealed class MatchCompleteConsumer(
    IConsumer<string, MatchCompleteMessage> consumer,
    IConnectionMultiplexer redis,
    ILogger<MatchCompleteConsumer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // unblock main task
        
        consumer.Subscribe(Constants.Kafka.MatchCompleteTopic);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                var msg = cr.Message.Value;
                var db = redis.GetDatabase();

                foreach (var uid in msg.UserIds)
                {
                    await db.StringSetAsync(
                        $"user:{uid}:match",
                        System.Text.Json.JsonSerializer.Serialize(msg),
                        expiry: TimeSpan.FromMinutes(30), // TODO: Use exiry from config
                        flags: CommandFlags.FireAndForget);
                }

                logger.MatchStored(msg.MatchId, msg.UserIds);

                consumer.Commit(cr);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Consumer loop failure");
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        consumer.Close();
        consumer.Dispose();
    }
}