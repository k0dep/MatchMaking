using Confluent.Kafka;
using FluentValidation;
using MatchMaking.Contracts.Dtos;
using MatchMaking.Service.Entities;
using MediatR;
using OneOf.Types;
using MatchSearchRequest = MatchMaking.Service.Entities.MatchSearchRequest;

namespace MatchMaking.Service.Handlers;

public class MatchSearchRequestHandler(
    IProducer<string, MatchSearchRequestMessage> producer,
    IValidator<MatchSearchRequest> validator,
    ILogger<MatchSearchRequestHandler> logger)
    : IRequestHandler<MatchSearchRequest, MatchSearchResponse>
{
    public async Task<MatchSearchResponse> Handle(MatchSearchRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.MatchSearchRequestStarting(request.UserId);

        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return new ValidationError(validationResult.Errors.Select(x => x.ErrorMessage));
        }

        await producer.ProduceAsync(
            Constants.Kafka.MatchRequestTopic,
            new Message<string, MatchSearchRequestMessage>
            {
                Key = request.UserId,
                Value = new MatchSearchRequestMessage(request.UserId)
            }, ct);

        return new Success();
    }
}