using Meezan.Application.Common.Results;
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
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;
using Meezan.Application.Features.Funds.Categories.Dtos;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Details.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFunds;

public sealed class GetFundsQueryHandler : IRequestHandler<GetFundsQuery, Result<FundListResultDto>>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundReadQuery _readQuery;
    private readonly IFundDetailQuery _detailQuery;
    private readonly IFundClassificationService _classificationService;

    public GetFundsQueryHandler(
        IFundRepository fundRepo,
        IFundReadQuery readQuery,
        IFundDetailQuery detailQuery,
        IFundClassificationService classificationService)
    {
        _fundRepo = fundRepo;
        _readQuery = readQuery;
        _detailQuery = detailQuery;
        _classificationService = classificationService;
    }

    private static string? MapLegacyFilterSlug(string slug)
    {
        var s = slug.Trim().ToLowerInvariant();
        return s switch
        {
            "commodities" => "silver",
            "index" => "index",
            "actively-managed" => "active",
            "unknown" => null,
            "index-egx30" or "index-egx33" or "index-egx70" or "index-egx100" or "tracks-egx35" => s,
            _ => s
        };
    }

    public async Task<Result<FundListResultDto>> Handle(GetFundsQuery request, CancellationToken cancellationToken)
    {
        var hasClassificationFilter = !string.IsNullOrWhiteSpace(request.Node)
            || !string.IsNullOrWhiteSpace(request.SubType)
            || !string.IsNullOrWhiteSpace(request.Leaf)
            || !string.IsNullOrWhiteSpace(request.Type);

        List<Fund> items;
        int totalCount;

        if (!hasClassificationFilter)
        {
            var filter = new FundListFilter
            {
                Search = request.Search,
                Type = null,
                RiskLevel = request.RiskLevel,
                AssetManagerId = request.AssetManagerId,
                IsIslamic = request.IsIslamic,
                IsFeatured = request.IsFeatured,
                CategoryId = request.CategoryId,
                MinManagementFee = request.MinManagementFee,
                MaxManagementFee = request.MaxManagementFee,
                MinInvestment = request.MinInvestment,
                MaxInvestment = request.MaxInvestment,
                SortBy = request.SortBy,
                SortDir = request.SortDir,
                Page = request.Page,
                PageSize = request.PageSize
            };
            (items, totalCount) = await _fundRepo.QueryPagedAsync(filter, cancellationToken);
        }
        else
        {
            var preFilter = new FundListFilter
            {
                Search = request.Search,
                Type = null,
                RiskLevel = request.RiskLevel,
                AssetManagerId = request.AssetManagerId,
                IsIslamic = request.IsIslamic,
                IsFeatured = request.IsFeatured,
                CategoryId = request.CategoryId,
                MinManagementFee = request.MinManagementFee,
                MaxManagementFee = request.MaxManagementFee,
                MinInvestment = request.MinInvestment,
                MaxInvestment = request.MaxInvestment,
                SortBy = null,
                SortDir = request.SortDir,
                Page = 1,
                PageSize = 10000
            };
            var (candidates, _) = await _fundRepo.QueryPagedAsync(preFilter, cancellationToken);

            var ctx = await _detailQuery.LoadContextAsync(cancellationToken);
            var eff = await _detailQuery.ResolveEffectiveManyAsync(candidates, ctx);

            HashSet<int>? allowed = null;
            bool empty = false;

            async Task<HashSet<int>?> ResolveSet(string? slug, bool descendants)
            {
                if (string.IsNullOrWhiteSpace(slug)) return null;
                var mapped = MapLegacyFilterSlug(slug.Trim());
                if (mapped == null) return new HashSet<int>();
                if (!ctx.BySlug.TryGetValue(mapped, out var n) || n.IsDeleted) return new HashSet<int>();
                var set = new HashSet<int> { n.Id };
                if (descendants)
                {
                    foreach (var d in await _classificationService.GetDescendantIdsAsync(n.Id, cancellationToken))
                        set.Add(d);
                }
                return set;
            }

            foreach (var (slug, desc) in new[] { (request.Node, true), (request.Type, true), (request.SubType, true), (request.Leaf, false) })
            {
                var set = await ResolveSet(slug, desc);
                if (set == null) continue;
                if (set.Count == 0) { empty = true; break; }
                allowed = allowed == null ? set : new HashSet<int>(allowed.Where(set.Contains));
                if (allowed.Count == 0) { empty = true; break; }
            }

            var matched = new List<(Fund Fund, EffectiveClassification Eff)>();
            if (!empty)
            {
                foreach (var f in candidates)
                {
                    if (!eff.TryGetValue(f.Id, out var e) || !e.NodeId.HasValue) continue;
                    if (allowed != null && !allowed.Contains(e.NodeId.Value)) continue;
                    matched.Add((f, e));
                }
            }

            var descending = request.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
            matched = (request.SortBy?.Trim().ToLowerInvariant()) switch
            {
                "return1m" => descending ? matched.OrderByDescending(x => x.Fund.Return1M).ToList() : matched.OrderBy(x => x.Fund.Return1M).ToList(),
                "return3m" => descending ? matched.OrderByDescending(x => x.Fund.Return3M).ToList() : matched.OrderBy(x => x.Fund.Return3M).ToList(),
                "return6m" => descending ? matched.OrderByDescending(x => x.Fund.Return6M).ToList() : matched.OrderBy(x => x.Fund.Return6M).ToList(),
                "return3y" => descending ? matched.OrderByDescending(x => x.Fund.Return3Y).ToList() : matched.OrderBy(x => x.Fund.Return3Y).ToList(),
                "aum" => descending ? matched.OrderByDescending(x => x.Fund.Aum).ToList() : matched.OrderBy(x => x.Fund.Aum).ToList(),
                "managementfee" => descending ? matched.OrderByDescending(x => x.Fund.ManagementFee).ToList() : matched.OrderBy(x => x.Fund.ManagementFee).ToList(),
                "totalscore" => descending ? matched.OrderByDescending(x => x.Fund.TotalScore).ToList() : matched.OrderBy(x => x.Fund.TotalScore).ToList(),
                "name" => descending ? matched.OrderByDescending(x => x.Fund.NameAr).ToList() : matched.OrderBy(x => x.Fund.NameAr).ToList(),
                "currentprice" => descending ? matched.OrderByDescending(x => x.Fund.CurrentPrice).ToList() : matched.OrderBy(x => x.Fund.CurrentPrice).ToList(),
                _ => descending ? matched.OrderByDescending(x => x.Fund.Return1Y).ToList() : matched.OrderBy(x => x.Fund.Return1Y).ToList(),
            };

            totalCount = matched.Count;
            items = matched.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).Select(x => x.Fund).ToList();
        }

        var (overrides, defs) = await _readQuery.LoadEffectiveTypesAsync(items.Select(f => f.Id).ToList(), cancellationToken);

        var classCtx = await _detailQuery.LoadContextAsync(cancellationToken);
        var classEff = await _detailQuery.ResolveEffectiveManyAsync(items, classCtx);

        var result = items.Select(f =>
        {
            var dto = new FundListItemDto
            {
                Id = f.Id,
                SndukId = f.SndukId,
                PublicId = f.PublicId,
                Slug = f.Slug,
                NameAr = f.NameAr,
                NameEn = f.NameEn,
                Code = f.Code,
                Type = f.Type,
                TypeNameAr = f.TypeNameAr,
                TypeNameEn = f.TypeNameEn,
                RiskLevel = f.RiskLevel,
                IsIslamic = f.IsIslamic,
                IsFeatured = f.IsFeatured,
                CurrentPrice = f.CurrentPrice,
                DailyChange = f.DailyChange,
                Return1M = f.Return1M,
                Return3M = f.Return3M,
                Return6M = f.Return6M,
                Return1Y = f.Return1Y,
                Return3Y = f.Return3Y,
                TotalScore = f.TotalScore,
                Aum = f.Aum,
                ManagementFee = f.ManagementFee,
                MinimumInvestment = f.MinimumInvestment,
                Currency = f.Currency,
                AssetManagerId = f.AssetManagerId,
                AssetManagerName = f.AssetManagerName,
                AssetManagerNameEn = f.AssetManagerNameEn,
                LogoUrl = f.LogoUrl,
                LocalLogoPath = f.LocalLogoPath,
                LastSyncedAt = f.LastSyncedAt
            };
            FundMappingExtensions.ApplyType(dto, f, overrides, defs);
            FundMappingExtensions.ApplyClassification(dto, f, classEff);
            return dto;
        }).ToList();

        return new FundListResultDto
        {
            Items = result,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        };
    }
}
