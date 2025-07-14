using Confluent.Kafka;
using MatchMaking.Contracts.Dtos;
using StackExchange.Redis;

namespace MatchMaking.Worker.Workers;

public sealed class MatchMakingWorker(
    IConsumer<string, MatchSearchRequestMessage> consumer,
    IProducer<string, MatchCompleteMessage> producer,
    IConnectionMultiplexer redis,
    IConfiguration cfg,
    ILogger<MatchMakingWorker> logger)
    : BackgroundService
{
    private readonly int _partySize = cfg.GetValue<int?>("Matchmaking:PartySize") ?? 3;

    private static readonly RedisKey PoolKey = "matchmaking:pool";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();// unblock main task
        consumer.Subscribe(Constants.Kafka.MatchRequestTopic);;
        var db = redis.GetDatabase();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                var uid = cr.Message.Value.UserId;

                // Try to add the user to the Redis set (returns false if already present)
                var added = await db.SetAddAsync(PoolKey, uid);
                if (added)
                {
                    var size = await db.SetLengthAsync(PoolKey);
                    if (size >= _partySize)
                    {
                        // Atomically pop the required number of players
                        var popped = (await db.SetPopAsync(PoolKey, _partySize))
                            .Select(rv => (string)rv!)
                            .ToArray();

                        if (popped.Length == _partySize)
                        {
                            var match = new MatchCompleteMessage(Guid.NewGuid().ToString(), popped);

                            await producer.ProduceAsync(
                                Constants.Kafka.MatchCompleteTopic,
                                new Message<string, MatchCompleteMessage>
                                {
                                    Key = match.MatchId,
                                    Value = match
                                }, stoppingToken);

                            logger.MatchFormed(match.MatchId, popped);
                        }
                    }
                }

                consumer.Commit(cr);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                logger.WorkerLoopError(ex);
                await Task.Delay(TimeSpan.FromMilliseconds(200), stoppingToken);
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