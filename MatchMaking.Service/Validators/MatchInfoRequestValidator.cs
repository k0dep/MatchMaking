using FluentValidation;
using MatchMaking.Service.Entities;

namespace MatchMaking.Service.Validators;

public class MatchInfoRequestValidator : AbstractValidator<MatchInfoRequest>
{
    public MatchInfoRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Length(1, 100);
    }
}