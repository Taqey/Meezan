using FluentValidation;

namespace Meezan.Application.Features.Funds.Types.Commands.RestoreFundType;

public sealed class RestoreFundTypeCommandValidator : AbstractValidator<RestoreFundTypeCommand>
{
    public RestoreFundTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
