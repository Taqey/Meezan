using FluentValidation;

namespace Meezan.Application.Features.Funds.Commands.CreateFundType;

public sealed class CreateFundTypeCommandValidator : AbstractValidator<CreateFundTypeCommand>
{
    public CreateFundTypeCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().WithMessage("Arabic name is required.");
        RuleFor(x => x.NameEn).NotEmpty().WithMessage("English name is required.");
    }
}
