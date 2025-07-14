using System.Text.Json;
using FluentValidation;
using MatchMaking.Service.Entities;
using MediatR;
using OneOf.Types;
using StackExchange.Redis;

namespace MatchMaking.Service.Handlers;

public class MatchInfoRequestHandler(
    ILogger<MatchInfoRequestHandler> logger,
    IConnectionMultiplexer redis,
    IValidator<MatchInfoRequest> validator)
    : IRequestHandler<MatchInfoRequest, MatchInfoResponse>
{

    public async Task<MatchInfoResponse> Handle(MatchInfoRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        logger.MatchInfoRequestStarting(request.UserId);
        
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return new ValidationError(validationResult.Errors.Select(x => x.ErrorMessage));
        }

        var db = redis.GetDatabase();
        var value = await db.StringGetAsync($"user:{request.UserId}:match", flags: CommandFlags.PreferReplica);

        if (value.IsNullOrEmpty)
        {
            return new NotFound();
        }
        
        return JsonSerializer.Deserialize<MatchInfo>(value.ToString())!;
    }
}