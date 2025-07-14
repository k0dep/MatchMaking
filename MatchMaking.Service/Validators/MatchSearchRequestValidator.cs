using FluentValidation;
using MatchMaking.Service.Entities;

namespace MatchMaking.Service.Validators;

public class MatchSearchRequestValidator : AbstractValidator<MatchSearchRequest>
{
    public MatchSearchRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Length(1, 100);
    }
}