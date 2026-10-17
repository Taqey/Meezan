using FluentValidation;

namespace Meezan.Application.Features.Funds.Types.Commands.UpdateFundType;

public sealed class UpdateFundTypeCommandValidator : AbstractValidator<UpdateFundTypeCommand>
{
    public UpdateFundTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
