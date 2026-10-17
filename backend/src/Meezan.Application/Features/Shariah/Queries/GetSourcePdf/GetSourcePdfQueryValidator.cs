using FluentValidation;

namespace Meezan.Application.Features.Shariah.Queries.GetSourcePdf;

public sealed class GetSourcePdfQueryValidator : AbstractValidator<GetSourcePdfQuery>
{
    public GetSourcePdfQueryValidator()
    {
        RuleFor(x => x.SourceKey).NotEmpty();
    }
}
