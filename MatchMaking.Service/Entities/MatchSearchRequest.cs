using MediatR;

namespace MatchMaking.Service.Entities;

public record MatchSearchRequest(string UserId) : IRequest<MatchSearchResponse>;