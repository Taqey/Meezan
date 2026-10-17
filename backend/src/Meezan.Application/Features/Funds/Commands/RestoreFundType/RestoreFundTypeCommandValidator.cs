using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.RestoreFundType;

public sealed class RestoreFundTypeCommandValidator : AbstractValidator<RestoreFundTypeCommand>
{
    public RestoreFundTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
