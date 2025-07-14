public static partial class Log
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Match {MatchId} formed (userIds: {@UserIds})")]
    public static partial void MatchFormed(this ILogger logger, string matchId, IEnumerable<string> userIds);

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Error,
        Message = "Worker loop error")]
    public static partial void WorkerLoopError(this ILogger logger, Exception exception);
}