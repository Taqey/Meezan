using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.DeleteFundType;

public sealed class DeleteFundTypeCommandValidator : AbstractValidator<DeleteFundTypeCommand>
{
    public DeleteFundTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
