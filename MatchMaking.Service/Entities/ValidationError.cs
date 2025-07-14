namespace MatchMaking.Service.Entities;

public record struct ValidationError(IEnumerable<string> Messages);