namespace MatchMaking.Service;

public static partial class Log
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Handling MatchInfoRequest for user {UserId}")]
    public static partial void MatchInfoRequestStarting(this ILogger logger, string userId);
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Trace,
        Message = "Handling MatchSearchRequest for user {UserId}")]
    public static partial void MatchSearchRequestStarting(this ILogger logger, string userId);
    
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Match {MatchId} stored (userIds: {@UserIds})")]
    public static partial void MatchStored(this ILogger logger, string matchId, IEnumerable<string> userIds);
}