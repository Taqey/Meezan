namespace Meezan.Application.Features.Funds.Categories.Queries.GetCategoryFunds;

using MediatR;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Domain.Entities;
using Meezan.Domain.Services;

public class GetCategoryFundsQueryHandler : IRequestHandler<GetCategoryFundsQuery, CategoryFundsResultDto?>
{
    private readonly IFundCategoryRepository _categoryRepo;
    private readonly IFundRepository _fundRepo;
    private readonly IFundCategoryOverrideRepository _overrideRepo;

    public GetCategoryFundsQueryHandler(
        IFundCategoryRepository categoryRepo,
        IFundRepository fundRepo,
        IFundCategoryOverrideRepository overrideRepo)
    {
        _categoryRepo = categoryRepo;
        _fundRepo = fundRepo;
        _overrideRepo = overrideRepo;
    }

    public async Task<CategoryFundsResultDto?> Handle(GetCategoryFundsQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepo.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null) throw new Meezan.Application.Common.Exceptions.NotFoundException(nameof(FundCategory), request.CategoryId);
        if (category.IsDeleted) throw new Meezan.Application.Common.Exceptions.BadRequestException("Category is deleted.");

        var funds = await _fundRepo.GetAllAsync(cancellationToken);
        var overrides = await _overrideRepo.GetByCategoryIdAsync(request.CategoryId, cancellationToken);
        var byFund = overrides.ToDictionary(o => o.FundId);

        var q = (request.Search ?? string.Empty).Trim();
        var onlySource = (request.Source ?? string.Empty).Trim();
        var members = new List<CategoryFundMemberItemDto>();

        foreach (var f in funds.OrderBy(f => f.Code))
        {
            if (!string.IsNullOrWhiteSpace(q) &&
                !f.Code.Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !(f.NameAr ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) &&
                !(f.NameEn ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase))
                continue;

            string? memberSource = null;
            string reason = string.Empty;

            if (byFund.TryGetValue(f.Id, out var o))
            {
                if (o.Action == OverrideAction.Exclude) continue;
                memberSource = o.CreatedBy == "AutoSync" ? "Auto" : "Manual";
                reason = o.CreatedBy == "AutoSync"
                    ? (FundAutoClassification.Evaluate(f, category.AutoRuleJson, out var r) ? r : string.Empty)
                    : "تعديل يدوي";
            }
            else if (category.IsAutoDerived)
            {
                if (!FundAutoClassification.Evaluate(f, category.AutoRuleJson, out var r))
                    continue;
                memberSource = "Auto";
                reason = r;
            }
            else
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(onlySource) &&
                !string.Equals(memberSource, onlySource, StringComparison.OrdinalIgnoreCase))
                continue;

            members.Add(new CategoryFundMemberItemDto(
                f.Id, f.Code, f.NameAr ?? string.Empty, f.NameEn ?? string.Empty,
                memberSource, reason));
        }

        var totalCount = members.Count;
        var items = members
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new CategoryFundsResultDto(
            items,
            totalCount,
            request.Page,
            request.PageSize,
            (int)Math.Ceiling((double)totalCount / request.PageSize));
    }
}
