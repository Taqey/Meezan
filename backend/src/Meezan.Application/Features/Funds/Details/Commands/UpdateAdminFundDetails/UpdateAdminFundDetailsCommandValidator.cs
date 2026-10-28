using FluentValidation;

namespace Meezan.Application.Features.Funds.Details.Commands.UpdateAdminFundDetails;

public sealed class UpdateAdminFundDetailsCommandValidator : AbstractValidator<UpdateAdminFundDetailsCommand>
{
    public UpdateAdminFundDetailsCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("معرف الصندوق غير صالح.");

        RuleFor(x => x.NameAr)
            .NotEmpty()
            .WithMessage("الاسم الكامل للصندوق مطلوب.")
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("الاسم الكامل للصندوق مطلوب.")
            .MaximumLength(250)
            .WithMessage("الاسم الكامل لا يمكن أن يتجاوز 250 حرفاً.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(250)
            .WithMessage("اسم العرض لا يمكن أن يتجاوز 250 حرفاً.")
            .When(x => x.DisplayName != null);

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(10000)
            .WithMessage("وصف الصندوق لا يمكن أن يتجاوز 10000 حرف.")
            .When(x => x.DescriptionAr != null);
    }
}

