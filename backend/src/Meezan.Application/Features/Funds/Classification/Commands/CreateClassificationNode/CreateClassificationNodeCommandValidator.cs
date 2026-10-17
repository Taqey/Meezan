using FluentValidation;

namespace Meezan.Application.Features.Funds.Classification.Commands.CreateClassificationNode;

public sealed class CreateClassificationNodeCommandValidator : AbstractValidator<CreateClassificationNodeCommand>
{
    public CreateClassificationNodeCommandValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty()
            .WithMessage("الاسم بالعربية والإنجليزية مطلوبان.");

        RuleFor(x => x.NameEn)
            .NotEmpty()
            .WithMessage("الاسم بالعربية والإنجليزية مطلوبان.");

        When(x => !string.IsNullOrWhiteSpace(x.FundDisposition), () =>
        {
            RuleFor(x => x.FundDisposition!)
                .Must(d => string.Equals(d, "keep", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(d, "moveAll", StringComparison.OrdinalIgnoreCase))
                .WithMessage("fundDisposition يجب أن تكون keep أو moveAll.");
        });
    }
}
