namespace MatchMaking.Contracts.Dtos;

public sealed record MatchCompleteMessage(string MatchId, IReadOnlyList<string> UserIds);