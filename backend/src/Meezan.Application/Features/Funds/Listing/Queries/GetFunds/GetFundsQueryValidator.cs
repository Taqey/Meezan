using FluentValidation;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFunds;

public sealed class GetFundsQueryValidator : AbstractValidator<GetFundsQuery>
{
    private static readonly string[] AllowedSortBy =
    {
        "return1m", "return3m", "return6m", "return1y", "return3y",
        "aum", "managementfee", "totalscore", "name", "currentprice"
    };

    public GetFundsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SortDir)
            .Must(d => string.Equals(d, "asc", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(d, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDir must be 'asc' or 'desc'.");

        When(x => !string.IsNullOrWhiteSpace(x.SortBy), () =>
        {
            RuleFor(x => x.SortBy!)
                .Must(s => AllowedSortBy.Contains(s.Trim().ToLowerInvariant()))
                .WithMessage("Invalid sortBy field.");
        });

        When(x => x.MinManagementFee.HasValue && x.MaxManagementFee.HasValue, () =>
        {
            RuleFor(x => x.MinManagementFee!.Value)
                .LessThanOrEqualTo(x => x.MaxManagementFee!.Value)
                .WithMessage("MinManagementFee cannot exceed MaxManagementFee.");
        });

        When(x => x.MinInvestment.HasValue && x.MaxInvestment.HasValue, () =>
        {
            RuleFor(x => x.MinInvestment!.Value)
                .LessThanOrEqualTo(x => x.MaxInvestment!.Value)
                .WithMessage("MinInvestment cannot exceed MaxInvestment.");
        });
    }
}
