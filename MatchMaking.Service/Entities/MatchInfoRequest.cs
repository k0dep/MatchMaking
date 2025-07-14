using MediatR;

namespace MatchMaking.Service.Entities;

public record MatchInfoRequest(string UserId) : IRequest<MatchInfoResponse>;