using MediatR;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Details;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;
using Meezan.Domain.Services;

namespace Meezan.Application.Features.Funds.Listing.Queries.GetFunds;

public sealed class GetFundsQueryHandler : IRequestHandler<GetFundsQuery, Result<FundListResultDto>>
{
    private readonly IFundRepository _fundRepo;
    private readonly IFundReadQuery _readQuery;
    private readonly IFundDetailQuery _detailQuery;
    private readonly IFundClassificationService _classificationService;
    private readonly Meezan.Application.Common.Interfaces.Caching.IFundCacheService _cacheService;
    private readonly Microsoft.Extensions.Options.IOptions<Meezan.Application.Common.Models.Caching.FundCacheOptions> _options;

    public GetFundsQueryHandler(
        IFundRepository fundRepo,
        IFundReadQuery readQuery,
        IFundDetailQuery detailQuery,
        IFundClassificationService classificationService,
        Meezan.Application.Common.Interfaces.Caching.IFundCacheService cacheService,
        Microsoft.Extensions.Options.IOptions<Meezan.Application.Common.Models.Caching.FundCacheOptions> options)
    {
        _fundRepo = fundRepo;
        _readQuery = readQuery;
        _detailQuery = detailQuery;
        _classificationService = classificationService;
        _cacheService = cacheService;
        _options = options;
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
        if (!_options.Value.Enabled)
        {
            return await HandleDirectDbQueryAsync(request, cancellationToken);
        }

        // Target Design: Static funds and classification from cache
        var staticFundsMap = await _cacheService.GetStaticFundsAsync(cancellationToken);
        var classDataset = await _cacheService.GetClassificationDatasetAsync(cancellationToken);

        // Resolve node filter to allowed fund ids
        HashSet<int>? allowedFundIds = null;
        bool isEmptyFilter = false;

        HashSet<int>? ResolveNodeFundIds(string? slug, bool descendants)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            var mapped = MapLegacyFilterSlug(slug.Trim());
            if (mapped == null) return new HashSet<int>();
            if (!classDataset.SlugToNodeId.TryGetValue(mapped, out var nodeId)) return new HashSet<int>();

            if (descendants)
            {
                return classDataset.NodeDescendantFundIds.TryGetValue(nodeId, out var set) ? set : new HashSet<int>();
            }
            else
            {
                var direct = classDataset.EffectiveByFundId
                    .Where(kv => kv.Value.NodeId == nodeId)
                    .Select(kv => kv.Key)
                    .ToHashSet();
                return direct;
            }
        }

        foreach (var (slug, desc) in new[] { (request.Node, true), (request.Type, true), (request.SubType, true), (request.Leaf, false) })
        {
            var set = ResolveNodeFundIds(slug, desc);
            if (set == null) continue;
            if (set.Count == 0) { isEmptyFilter = true; break; }
            allowedFundIds = allowedFundIds == null ? set : new HashSet<int>(allowedFundIds.Where(set.Contains));
            if (allowedFundIds.Count == 0) { isEmptyFilter = true; break; }
        }

        var candidates = new List<Meezan.Application.Common.Models.Caching.CachedFundStatic>();

        FundRiskLevel? parsedRiskLevel = null;
        if (!string.IsNullOrWhiteSpace(request.RiskLevel))
        {
            if (Enum.TryParse<FundRiskLevel>(request.RiskLevel.Trim(), true, out var parsed) && Enum.IsDefined(parsed))
            {
                parsedRiskLevel = parsed;
            }
            else
            {
                return Result<FundListResultDto>.Success(new FundListResultDto
                {
                    Items = [],
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = 0
                });
            }
        }

        if (!isEmptyFilter)
        {
            foreach (var (fId, sf) in staticFundsMap)
            {
                if (!sf.IsActive) continue;
                if (allowedFundIds != null && !allowedFundIds.Contains(fId)) continue;

                // Search query
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var q = request.Search.Trim();
                    var match = (sf.NameAr != null && sf.NameAr.Contains(q, StringComparison.OrdinalIgnoreCase))
                        || (sf.DisplayName != null && sf.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase))
                        || (sf.NameEn != null && sf.NameEn.Contains(q, StringComparison.OrdinalIgnoreCase))
                        || (sf.Code != null && sf.Code.Contains(q, StringComparison.OrdinalIgnoreCase))
                        || (sf.AssetManagerName != null && sf.AssetManagerName.Contains(q, StringComparison.OrdinalIgnoreCase));
                    if (!match) continue;
                }

                // Attributes
                if (parsedRiskLevel.HasValue && sf.RiskLevel != parsedRiskLevel.Value) continue;
                if (request.AssetManagerId.HasValue && sf.AssetManagerId != request.AssetManagerId.Value) continue;
                if (request.IsIslamic.HasValue && sf.IsIslamic != request.IsIslamic.Value) continue;
                if (request.IsFeatured.HasValue && sf.IsFeatured != request.IsFeatured.Value) continue;
                if (request.CategoryId.HasValue && !sf.CategoryIds.Contains(request.CategoryId.Value)) continue;

                if (request.MinManagementFee.HasValue && (sf.ManagementFee ?? 0) < request.MinManagementFee.Value) continue;
                if (request.MaxManagementFee.HasValue && (sf.ManagementFee ?? 0) > request.MaxManagementFee.Value) continue;

                candidates.Add(sf);
            }
        }

        var isDesc = request.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var sortField = request.SortBy?.Trim().ToLowerInvariant();
        bool isPriceSort = sortField is "return1m" or "return3m" or "return6m" or "return1y" or "return3y" or "currentprice" || string.IsNullOrWhiteSpace(sortField);

        Dictionary<int, Meezan.Application.Common.Models.Funds.FundPriceDto> candidatePrices;
        if (isPriceSort && candidates.Count > 0)
        {
            // If sorted by price/return fields, query prices for all matching candidates in ONE SQL query
            candidatePrices = await _readQuery.GetPricesForFundsAsync(candidates.Select(c => c.Id).ToList(), cancellationToken);
        }
        else
        {
            candidatePrices = new Dictionary<int, Meezan.Application.Common.Models.Funds.FundPriceDto>();
        }

        candidates = sortField switch
        {
            "return1m" => isDesc ? candidates.OrderByDescending(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return1M : null).ToList() : candidates.OrderBy(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return1M : null).ToList(),
            "return3m" => isDesc ? candidates.OrderByDescending(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return3M : null).ToList() : candidates.OrderBy(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return3M : null).ToList(),
            "return6m" => isDesc ? candidates.OrderByDescending(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return6M : null).ToList() : candidates.OrderBy(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return6M : null).ToList(),
            "return3y" => isDesc ? candidates.OrderByDescending(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return3Y : null).ToList() : candidates.OrderBy(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return3Y : null).ToList(),
            "aum" => isDesc ? candidates.OrderByDescending(x => x.Aum).ToList() : candidates.OrderBy(x => x.Aum).ToList(),
            "managementfee" => isDesc ? candidates.OrderByDescending(x => x.ManagementFee).ToList() : candidates.OrderBy(x => x.ManagementFee).ToList(),
            "totalscore" => isDesc ? candidates.OrderByDescending(x => x.TotalScore).ToList() : candidates.OrderBy(x => x.TotalScore).ToList(),
            "name" => isDesc ? candidates.OrderByDescending(x => x.NameAr).ToList() : candidates.OrderBy(x => x.NameAr).ToList(),
            "currentprice" => isDesc ? candidates.OrderByDescending(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.CurrentPrice : null).ToList() : candidates.OrderBy(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.CurrentPrice : null).ToList(),
            _ => isDesc ? candidates.OrderByDescending(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return1Y : null).ToList() : candidates.OrderBy(x => candidatePrices.TryGetValue(x.Id, out var p) ? p.Return1Y : null).ToList(),
        };

        var totalCount = candidates.Count;
        var pageItems = candidates.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();
        var pageIds = pageItems.Select(x => x.Id).ToList();

        // If not already fetched for all candidates, fetch prices for the current page in ONE SQL query
        Dictionary<int, Meezan.Application.Common.Models.Funds.FundPriceDto> pagePrices;
        if (isPriceSort && candidates.Count > 0)
        {
            pagePrices = candidatePrices;
        }
        else
        {
            pagePrices = await _readQuery.GetPricesForFundsAsync(pageIds, cancellationToken);
        }

        var result = pageItems.Select(f =>
        {
            pagePrices.TryGetValue(f.Id, out var p);
            var eff = f.EffectiveClassification;

            var dto = new FundListItemDto
            {
                Id = f.Id,
                SndukId = f.SndukId,
                PublicId = f.PublicId,
                Slug = f.Slug,
                NameAr = f.NameAr,
                DisplayName = !string.IsNullOrWhiteSpace(f.DisplayName) ? f.DisplayName : f.NameAr,
                FullNameAr = f.NameAr,
                NameEn = f.NameEn,
                DescriptionAr = f.DescriptionAr,
                IsNameManuallyEdited = f.IsNameManuallyEdited,
                IsDescriptionManuallyEdited = f.IsDescriptionManuallyEdited,
                Code = f.Code,
                Type = eff != null && !string.IsNullOrWhiteSpace(eff.RootSlug) ? FundTypeCatalog.EnumFor(eff.RootSlug) : f.Type,
                TypeNameAr = eff != null && !string.IsNullOrWhiteSpace(eff.RootNameAr) ? eff.RootNameAr : f.TypeNameAr,
                TypeNameEn = eff != null && !string.IsNullOrWhiteSpace(eff.RootNameEn) ? eff.RootNameEn : f.TypeNameEn,
                TypeSlug = eff != null && !string.IsNullOrWhiteSpace(eff.RootSlug) ? eff.RootSlug : null,
                EffectiveClassification = eff,
                RiskLevel = f.RiskLevel,
                IsIslamic = f.IsIslamic,
                IsFeatured = f.IsFeatured,
                CurrentPrice = p?.CurrentPrice,
                DailyChange = p?.DailyChange,
                Return1M = p?.Return1M,
                Return3M = p?.Return3M,
                Return6M = p?.Return6M,
                Return1Y = p?.Return1Y,
                Return3Y = p?.Return3Y,
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
            return dto;
        }).ToList();

        return Result<FundListResultDto>.Success(new FundListResultDto
        {
            Items = result,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        });
    }

    private async Task<Result<FundListResultDto>> HandleDirectDbQueryAsync(GetFundsQuery request, CancellationToken cancellationToken)
    {
        var filter = new FundListFilter
        {
            Search = request.Search,
            Type = request.Type,
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

        var (items, totalCount) = await _fundRepo.QueryPagedAsync(filter, cancellationToken);
        var pageIds = items.Select(x => x.Id).ToList();
        var pricesMap = await _readQuery.GetPricesForFundsAsync(pageIds, cancellationToken);
        var (overrides, defs) = await _readQuery.LoadEffectiveTypesAsync(pageIds, cancellationToken);
        var classCtx = await _detailQuery.LoadContextAsync(cancellationToken);
        var classEff = await _detailQuery.ResolveEffectiveManyAsync(items, classCtx);

        var result = items.Select(f =>
        {
            pricesMap.TryGetValue(f.Id, out var p);
            var dto = new FundListItemDto
            {
                Id = f.Id,
                SndukId = f.SndukId,
                PublicId = f.PublicId,
                Slug = f.Slug,
                NameAr = f.NameAr,
                DisplayName = !string.IsNullOrWhiteSpace(f.DisplayName) ? f.DisplayName : f.NameAr,
                FullNameAr = f.NameAr,
                NameEn = f.NameEn,
                DescriptionAr = f.DescriptionAr,
                IsNameManuallyEdited = f.IsNameManuallyEdited,
                IsDescriptionManuallyEdited = f.IsDescriptionManuallyEdited,
                Code = f.Code,
                Type = f.Type,
                TypeNameAr = f.TypeNameAr,
                TypeNameEn = f.TypeNameEn,
                RiskLevel = f.RiskLevel,
                IsIslamic = f.IsIslamic,
                IsFeatured = f.IsFeatured,
                CurrentPrice = p?.CurrentPrice ?? f.CurrentPrice,
                DailyChange = p?.DailyChange ?? f.DailyChange,
                Return1M = p?.Return1M ?? f.Return1M,
                Return3M = p?.Return3M ?? f.Return3M,
                Return6M = p?.Return6M ?? f.Return6M,
                Return1Y = p?.Return1Y ?? f.Return1Y,
                Return3Y = p?.Return3Y ?? f.Return3Y,
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

        return Result<FundListResultDto>.Success(new FundListResultDto
        {
            Items = result,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
        });
    }
}
