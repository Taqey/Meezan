using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Models.Caching;
using Meezan.Application.Features.Funds.Classification.Dtos;
using Meezan.Application.Features.Funds.Listing.Dtos;
using Meezan.Application.Features.Funds.Types.Dtos;
using Meezan.Domain.Entities;
using Meezan.Domain.Models;
using Meezan.Domain.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Meezan.Infrastructure.Services.Caching;

public class FundCacheService : IFundCacheService
{
    private const string CacheSchemaVersion = "v1";
    private readonly IServiceProvider _services;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly RedisOptions _options;
    private readonly ILogger<FundCacheService> _logger;
    private readonly string _prefix;

    // Concurrency flight guards
    private readonly SemaphoreSlim _classificationLock = new(1, 1);
    private readonly SemaphoreSlim _staticFundsLock = new(1, 1);
    private readonly SemaphoreSlim _pricesSnapshotLock = new(1, 1);

    // Metrics counters
    private long _l1Hits;
    private long _l1Misses;
    private long _l2Hits;
    private long _l2Misses;
    private DateTime? _lastRebuildUtc;

    // Circuit breaker state
    private DateTime? _circuitBreakerUntilUtc;
    private DateTime? _lastRedisErrorLoggedUtc;

    public FundCacheService(
        IServiceProvider services,
        IMemoryCache memoryCache,
        IOptions<RedisOptions> options,
        IConfiguration configuration,
        ILogger<FundCacheService> logger,
        IConnectionMultiplexer? redis = null)
    {
        _services = services;
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
        _redis = redis;

        // KeyPrefix derivation: if empty, derive from db server + db name hash
        if (!string.IsNullOrWhiteSpace(_options.KeyPrefix))
        {
            _prefix = _options.KeyPrefix.TrimEnd(':');
        }
        else
        {
            var connStr = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(connStr))).Substring(0, 12).ToLowerInvariant();
            _prefix = $"meezan:{hash}";
        }

        SubscribePubSub();
    }

    private void SubscribePubSub()
    {
        if (_redis == null || !_redis.IsConnected) return;

        try
        {
            var sub = _redis.GetSubscriber();
            sub.Subscribe(RedisChannel.Literal($"{_prefix}:pubsub:invalidation"), (channel, message) =>
            {
                var msgStr = message.ToString();
                if (msgStr.Contains("catalog"))
                {
                    _memoryCache.Remove($"{_prefix}:L1:classification");
                    _memoryCache.Remove($"{_prefix}:L1:static_funds");
                    _memoryCache.Remove($"{_prefix}:L1:catalog_ver");
                }
                if (msgStr.Contains("prices"))
                {
                    _memoryCache.Remove($"{_prefix}:L1:prices_snapshot");
                    _memoryCache.Remove($"{_prefix}:L1:prices_ver");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to subscribe to Redis invalidation pubsub channel");
        }
    }

    private bool IsRedisAvailable()
    {
        if (_redis == null || !_redis.IsConnected) return false;
        if (_circuitBreakerUntilUtc.HasValue && DateTime.UtcNow < _circuitBreakerUntilUtc.Value) return false;
        return true;
    }

    private void RecordRedisFailure(Exception ex)
    {
        _circuitBreakerUntilUtc = DateTime.UtcNow.AddSeconds(30);

        var now = DateTime.UtcNow;
        if (!_lastRedisErrorLoggedUtc.HasValue || now - _lastRedisErrorLoggedUtc.Value > TimeSpan.FromSeconds(30))
        {
            _lastRedisErrorLoggedUtc = now;
            _logger.LogError(ex, "Redis operation failed. Circuit breaker opened for 30s. Falling back to L1/SQL.");
        }
    }

    private long _fallbackCatalogVersion = 1;
    private long _fallbackPricesVersion = 1;

    private async Task<long> GetDatasetVersionAsync(string dataset, CancellationToken ct)
    {
        // Check L1 version first (with 15s TTL check)
        var l1VerKey = $"{_prefix}:L1:{dataset}_ver";
        if (_memoryCache.TryGetValue<long>(l1VerKey, out var l1Ver))
        {
            return l1Ver;
        }

        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var key = $"{_prefix}:{CacheSchemaVersion}:version:{dataset}";
                var val = await db.StringGetAsync(key);
                if (val.HasValue && long.TryParse((string?)val, out var v))
                {
                    _memoryCache.Set(l1VerKey, v, TimeSpan.FromSeconds(15));
                    return v;
                }
                else
                {
                    await db.StringSetAsync(key, 1, TimeSpan.FromDays(30));
                    _memoryCache.Set(l1VerKey, 1L, TimeSpan.FromSeconds(15));
                    return 1;
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        var fallbackVer = dataset == "prices" ? _fallbackPricesVersion : _fallbackCatalogVersion;
        _memoryCache.Set(l1VerKey, fallbackVer, TimeSpan.FromSeconds(15));
        return fallbackVer;
    }

    private string BuildKey(string dataset, long version)
    {
        return $"{_prefix}:{CacheSchemaVersion}:{dataset}:v{version}";
    }

    public async Task<CachedClassificationDataset> GetClassificationDatasetAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var ver = await GetDatasetVersionAsync("catalog", ct);
        var l1Key = $"{_prefix}:L1:classification:v{ver}";

        if (_memoryCache.TryGetValue<CachedClassificationDataset>(l1Key, out var l1Val))
        {
            Interlocked.Increment(ref _l1Hits);
            return l1Val!;
        }

        Interlocked.Increment(ref _l1Misses);

        var l2Key = BuildKey("classification", ver);
        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var raw = await db.StringGetAsync(l2Key);
                if (raw.HasValue)
                {
                    Interlocked.Increment(ref _l2Hits);
                    var cached = JsonSerializer.Deserialize<CachedClassificationDataset>(raw.ToString());
                    if (cached != null)
                    {
                        _memoryCache.Set(l1Key, cached, TimeSpan.FromMinutes(10));
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        Interlocked.Increment(ref _l2Misses);

        // Single flight rebuild
        await _classificationLock.WaitAsync(ct);
        try
        {
            // Re-check after lock
            if (_memoryCache.TryGetValue<CachedClassificationDataset>(l1Key, out var recheck))
            {
                return recheck!;
            }

            var rebuilt = await RebuildClassificationDatasetAsync(ct);

            if (rebuilt == null || ct.IsCancellationRequested)
            {
                _logger.LogWarning("Classification dataset build yielded empty or cancelled results. Nothing stored in cache.");
                return rebuilt ?? new CachedClassificationDataset();
            }

            _memoryCache.Set(l1Key, rebuilt, TimeSpan.FromMinutes(10));

            if (IsRedisAvailable())
            {
                try
                {
                    var db = _redis!.GetDatabase();
                    var json = JsonSerializer.Serialize(rebuilt);
                    await db.StringSetAsync(l2Key, json, TimeSpan.FromDays(7));
                }
                catch (Exception ex)
                {
                    RecordRedisFailure(ex);
                }
            }

            _lastRebuildUtc = DateTime.UtcNow;
            return rebuilt;
        }
        finally
        {
            _classificationLock.Release();
        }
    }

    private async Task<CachedClassificationDataset> RebuildClassificationDatasetAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var nodeRepo = scope.ServiceProvider.GetRequiredService<IFundClassificationNodeRepository>();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
        var typeDefRepo = scope.ServiceProvider.GetRequiredService<IFundTypeDefinitionRepository>();
        var typeOverrideRepo = scope.ServiceProvider.GetRequiredService<IFundTypeOverrideRepository>();
        var detailQuery = scope.ServiceProvider.GetRequiredService<IFundDetailQuery>();

        var allNodes = await nodeRepo.GetAllAsync(false, ct);
        var visibleNodes = allNodes.Where(n => n.IsVisible && !n.IsDeleted).ToList();
        var allFunds = await fundRepo.GetAllStaticAsync(ct);
        var activeFunds = allFunds.Where(f => f.IsActive).ToList();

        var ctx = await detailQuery.LoadContextAsync(ct);

        // Build effective classifications
        var effectiveMap = new Dictionary<int, EffectiveClassificationDto>();
        foreach (var fund in activeFunds)
        {
            var eff = Meezan.Infrastructure.Services.Funds.Classification.FundClassificationService.ResolveEffective(fund, ctx);
            if (eff?.NodeId != null && ctx.ById.TryGetValue(eff.NodeId.Value, out FundClassificationNode? mNode) && mNode != null)
            {
                var pathNodes = FundClassificationTreeEngine.BuildPath(mNode, ctx.ById);
                var root = pathNodes.FirstOrDefault();
                var leaf = pathNodes.LastOrDefault();

                effectiveMap[fund.Id] = new EffectiveClassificationDto
                {
                    NodeId = eff.NodeId,
                    Slug = eff.Slug,
                    NameAr = mNode.NameAr,
                    NameEn = mNode.NameEn,
                    Path = string.Join(" > ", pathNodes.Select(p => p.NameAr)),
                    RootSlug = root?.Slug ?? eff.Slug,
                    RootNameAr = root?.NameAr ?? mNode.NameAr,
                    RootNameEn = root?.NameEn ?? mNode.NameEn,
                    LeafSlug = leaf?.Slug ?? eff.Slug,
                    LeafNameAr = leaf?.NameAr ?? mNode.NameAr,
                    LeafNameEn = leaf?.NameEn ?? mNode.NameEn,
                    Source = eff.Source,
                    PathSlugs = pathNodes.Select(p => p.Slug).ToList(),
                    PathNamesAr = pathNodes.Select(p => p.NameAr).ToList(),
                    PathNamesEn = pathNodes.Select(p => p.NameEn).ToList()
                };
            }
        }

        // Descendant fund maps
        var descendantFundsMap = new Dictionary<int, HashSet<int>>();
        foreach (var node in visibleNodes)
        {
            var descNodeIds = FundClassificationTreeEngine.DescendantIds(allNodes, node.Id);
            descNodeIds.Add(node.Id); // include self
            var fundIds = new HashSet<int>();
            foreach (var (fId, effDto) in effectiveMap)
            {
                if (effDto.NodeId.HasValue && descNodeIds.Contains(effDto.NodeId.Value))
                {
                    fundIds.Add(fId);
                }
            }
            descendantFundsMap[node.Id] = fundIds;
        }

        // Tree DTO
        var byId = allNodes.ToDictionary(n => n.Id);
        var tree = visibleNodes.Select(n => new FundClassificationTreeDto
        {
            Id = n.Id,
            Slug = n.Slug,
            NameAr = n.NameAr,
            NameEn = n.NameEn,
            Level = n.Level,
            ParentSlug = n.ParentId.HasValue && byId.TryGetValue(n.ParentId.Value, out var p) ? p.Slug : null,
            DisplayOrder = n.DisplayOrder,
            ChildrenLabelAr = n.ChildrenLabelAr,
            ChildrenLabelEn = n.ChildrenLabelEn,
            DirectFundCount = effectiveMap.Values.Count(e => e.NodeId == n.Id),
            TotalFundCount = descendantFundsMap.TryGetValue(n.Id, out var s) ? s.Count : 0,
            HasAutoRule = !string.IsNullOrWhiteSpace(n.AutoRuleJson)
        }).Where(t => t.TotalFundCount > 0).ToList();

        // Type definitions and overrides
        var allDefs = await typeDefRepo.GetAllAsync(false, ct);
        var defsBySlug = allDefs.ToDictionary(d => d.Slug, d => new CachedTypeDefinition
        {
            Id = d.Id,
            Slug = d.Slug,
            NameAr = d.NameAr,
            NameEn = d.NameEn,
            DisplayOrder = d.DisplayOrder,
            IsVisible = d.IsVisible,
            IsDeleted = d.IsDeleted
        });

        var typeOptions = allDefs
            .Where(t => t.IsVisible && !t.IsDeleted)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.NameAr)
            .Select(t => new FundTypeOptionDto
            {
                Id = t.Id,
                Slug = t.Slug,
                NameAr = t.NameAr,
                NameEn = t.NameEn,
                DisplayOrder = t.DisplayOrder
            }).ToList();

        var overrides = await typeOverrideRepo.GetAllFundOverridesAsync(ct);
        var typeOverrides = overrides.ToDictionary(
            kv => kv.Key,
            kv => (kv.Value, defsBySlug.Values.FirstOrDefault(d => d.Id == kv.Value)?.Slug ?? string.Empty));

        // Stats
        var totalCount = activeFunds.Count;
        var islamicCount = activeFunds.Count(f => f.IsIslamic);
        var totalAum = (double)activeFunds.Sum(f => f.Aum ?? 0);
        var bestFund = activeFunds.Where(f => f.DailyChange != null).OrderByDescending(f => f.DailyChange).FirstOrDefault();

        // Asset managers
        var assetManagers = activeFunds
            .Where(f => f.AssetManagerId.HasValue && !string.IsNullOrWhiteSpace(f.AssetManagerName))
            .GroupBy(f => new { Id = f.AssetManagerId!.Value, NameAr = f.AssetManagerName, NameEn = f.AssetManagerNameEn })
            .Select(g => new FundAssetManagerOptionDto
            {
                Id = g.Key.Id,
                NameAr = g.Key.NameAr,
                NameEn = g.Key.NameEn,
                FundCount = g.Count()
            })
            .OrderByDescending(m => m.FundCount)
            .ThenBy(m => m.NameAr)
            .ToList();

        return new CachedClassificationDataset
        {
            Tree = tree,
            EffectiveByFundId = effectiveMap,
            NodeDescendantFundIds = descendantFundsMap,
            SlugToNodeId = visibleNodes.ToDictionary(n => n.Slug, n => n.Id, StringComparer.OrdinalIgnoreCase),
            TypeOptions = typeOptions,
            TypeDefsBySlug = defsBySlug,
            TypeOverrides = typeOverrides,
            AssetManagers = assetManagers,
            Stats = new FundStatsDto
            {
                TotalCount = totalCount,
                IslamicCount = islamicCount,
                TotalAum = totalAum,
                BestDailyChange = bestFund != null ? (double?)bestFund.DailyChange : null,
                BestDailyChangeCode = bestFund?.Code
            }
        };
    }

    private DateTime? _staticBuildCooldownUntilUtc;
    private int _isStaticRebuilding = 0;

    public async Task<Dictionary<int, CachedFundStatic>> GetStaticFundsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var ver = await GetDatasetVersionAsync("catalog", ct);
        var l1Key = $"{_prefix}:L1:static_funds:v{ver}";

        if (_memoryCache.TryGetValue<Dictionary<int, CachedFundStatic>>(l1Key, out var l1Val))
        {
            if (l1Val != null && l1Val.Count > 0)
            {
                Interlocked.Increment(ref _l1Hits);
                return l1Val;
            }
            // Incomplete/empty entry treated as a miss
            _memoryCache.Remove(l1Key);
        }

        Interlocked.Increment(ref _l1Misses);

        var l2Key = BuildKey("static_funds", ver);
        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var raw = await db.StringGetAsync(l2Key);
                if (raw.HasValue)
                {
                    var cached = JsonSerializer.Deserialize<Dictionary<int, CachedFundStatic>>(raw.ToString());
                    if (cached != null && cached.Count > 0)
                    {
                        Interlocked.Increment(ref _l2Hits);
                        _memoryCache.Set(l1Key, cached, TimeSpan.FromMinutes(10));
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        Interlocked.Increment(ref _l2Misses);

        // Cooldown guard: if a previous build failed within 30s, fallback to direct SQL path
        if (_staticBuildCooldownUntilUtc.HasValue && DateTime.UtcNow < _staticBuildCooldownUntilUtc.Value)
        {
            _logger.LogWarning("Fund static cache build cooldown active until {CooldownUntil}. Falling back to direct build without caching.", _staticBuildCooldownUntilUtc.Value);
            return await ExecuteDirectStaticFundsQueryAsync(ct);
        }

        // Single-flight per key with cancellation token
        await _staticFundsLock.WaitAsync(ct);
        try
        {
            if (_memoryCache.TryGetValue<Dictionary<int, CachedFundStatic>>(l1Key, out var recheck) && recheck != null && recheck.Count > 0)
            {
                return recheck;
            }

            // Re-entrancy guard
            if (Interlocked.CompareExchange(ref _isStaticRebuilding, 1, 0) != 0)
            {
                _logger.LogWarning("Re-entrancy detected in static funds build. Falling back to direct query.");
                return await ExecuteDirectStaticFundsQueryAsync(ct);
            }

            try
            {
                var rebuilt = await RebuildStaticFundsAsync(ct);

                // Incomplete or empty build must store NOTHING
                if (rebuilt == null || rebuilt.Count == 0 || ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Static funds build yielded empty or cancelled results. Nothing stored in cache.");
                    return rebuilt ?? new Dictionary<int, CachedFundStatic>();
                }

                _memoryCache.Set(l1Key, rebuilt, TimeSpan.FromMinutes(10));

                if (IsRedisAvailable())
                {
                    try
                    {
                        var db = _redis!.GetDatabase();
                        var json = JsonSerializer.Serialize(rebuilt);
                        await db.StringSetAsync(l2Key, json, TimeSpan.FromDays(7));
                    }
                    catch (Exception ex)
                    {
                        RecordRedisFailure(ex);
                    }
                }

                _lastRebuildUtc = DateTime.UtcNow;
                _staticBuildCooldownUntilUtc = null;
                return rebuilt;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Static funds cache build was cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                _staticBuildCooldownUntilUtc = DateTime.UtcNow.AddSeconds(30);
                _logger.LogError(ex, "Failed to rebuild static funds cache. 30s cooldown applied. Falling back to direct SQL query.");
                return await ExecuteDirectStaticFundsQueryAsync(ct);
            }
            finally
            {
                Interlocked.Exchange(ref _isStaticRebuilding, 0);
            }
        }
        finally
        {
            _staticFundsLock.Release();
        }
    }

    private async Task<Dictionary<int, CachedFundStatic>> ExecuteDirectStaticFundsQueryAsync(CancellationToken ct)
    {
        return await RebuildStaticFundsAsync(ct);
    }

    private async Task<Dictionary<int, CachedFundStatic>> RebuildStaticFundsAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
        var analysisRepo = scope.ServiceProvider.GetRequiredService<IFundAnalysisRepository>();
        var similarRepo = scope.ServiceProvider.GetRequiredService<IFundSimilarRepository>();
        var overrideRepo = scope.ServiceProvider.GetRequiredService<IFundCategoryOverrideRepository>();

        // Set-based bulk queries: constant number of queries (4 queries here, well under 10)
        // 1. Static fund columns only (no price fields)
        var allFunds = await fundRepo.GetAllStaticAsync(ct);

        // 2. All analyses in one bulk query
        var allAnalyses = await analysisRepo.GetAllAsync(ct);
        var analysesByFundId = allAnalyses.ToDictionary(a => a.FundId);

        // 3. Similar fund links in one bulk query (FundId, SimilarFundId only, no fund entity joins)
        var allSimilarLinks = await similarRepo.GetAllSimilarLinksAsync(ct);
        var similarsByFundId = allSimilarLinks
            .GroupBy(s => s.FundId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.SimilarFundId).ToList());

        // 4. Category overrides in one bulk query
        var allOverrides = await overrideRepo.GetAllAsync(ct);
        var overridesByFundId = allOverrides
            .Where(o => o.Action == OverrideAction.Include)
            .GroupBy(o => o.FundId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CategoryId).ToList());

        // Classification dataset: nodes and type defs
        var classDataset = await GetClassificationDatasetAsync(ct);

        var result = new Dictionary<int, CachedFundStatic>(allFunds.Count);

        foreach (var f in allFunds)
        {
            CachedFundAnalysis? cachedAnalysis = null;
            if (analysesByFundId.TryGetValue(f.Id, out var analysis) && analysis != null)
            {
                cachedAnalysis = new CachedFundAnalysis
                {
                    PerformanceScore = analysis.PerformanceScore,
                    RiskScore = analysis.RiskScore,
                    CostScore = analysis.CostScore,
                    StabilityScore = analysis.StabilityScore,
                    DiversificationScore = analysis.DiversificationScore,
                    TotalScore = analysis.TotalScore,
                    Cagr = analysis.Cagr,
                    Alpha = analysis.Alpha,
                    Volatility = analysis.Volatility,
                    Consistency = analysis.Consistency,
                    MaxDrawdown = analysis.MaxDrawdown,
                    SharpeRatio = analysis.SharpeRatio,
                    WeightsJson = analysis.WeightsJson,
                    PerformanceExplanationAr = analysis.PerformanceExplanationAr,
                    PerformanceExplanationEn = analysis.PerformanceExplanationEn,
                    RiskExplanationAr = analysis.RiskExplanationAr,
                    RiskExplanationEn = analysis.RiskExplanationEn,
                    CostExplanationAr = analysis.CostExplanationAr,
                    CostExplanationEn = analysis.CostExplanationEn,
                    StabilityExplanationAr = analysis.StabilityExplanationAr,
                    StabilityExplanationEn = analysis.StabilityExplanationEn,
                    DiversificationExplanationAr = analysis.DiversificationExplanationAr,
                    DiversificationExplanationEn = analysis.DiversificationExplanationEn,
                    CalculatedAt = analysis.CalculatedAt
                };
            }

            classDataset.EffectiveByFundId.TryGetValue(f.Id, out var eff);
            similarsByFundId.TryGetValue(f.Id, out var simList);
            overridesByFundId.TryGetValue(f.Id, out var catList);

            result[f.Id] = new CachedFundStatic
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
                IsActive = f.IsActive,
                TotalScore = f.TotalScore,
                Aum = f.Aum,
                ManagementFee = f.ManagementFee,
                MinimumInvestment = f.MinimumInvestment,
                Currency = f.Currency,
                AssetManagerId = f.AssetManagerId,
                AssetManagerName = f.AssetManagerName,
                AssetManagerNameEn = f.AssetManagerNameEn,
                AssetManagerCode = f.AssetManagerCode,
                LogoUrl = f.LogoUrl,
                LocalLogoPath = f.LocalLogoPath,
                DescriptionAr = f.DescriptionAr,
                DescriptionEn = f.DescriptionEn,
                InvestmentStrategy = f.InvestmentStrategy,
                InceptionDate = f.InceptionDate,
                ProspectusUrl = f.ProspectusUrl,
                LastSyncedAt = f.LastSyncedAt,
                EffectiveClassification = eff,
                Analysis = cachedAnalysis,
                SimilarFundIds = simList ?? new List<int>(),
                CategoryIds = catList ?? new List<int>()
            };
        }

        return result;
    }

    public async Task<Dictionary<int, CachedFundPriceSnapshot>> GetPricesSnapshotAsync(CancellationToken ct = default)
    {
        var ver = await GetDatasetVersionAsync("prices", ct);
        var l1Key = $"{_prefix}:L1:prices_snapshot:v{ver}";

        if (_memoryCache.TryGetValue<Dictionary<int, CachedFundPriceSnapshot>>(l1Key, out var l1Val))
        {
            Interlocked.Increment(ref _l1Hits);
            return l1Val!;
        }

        Interlocked.Increment(ref _l1Misses);

        var l2Key = BuildKey("prices_snapshot", ver);
        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var raw = await db.StringGetAsync(l2Key);
                if (raw.HasValue)
                {
                    Interlocked.Increment(ref _l2Hits);
                    var cached = JsonSerializer.Deserialize<Dictionary<int, CachedFundPriceSnapshot>>(raw.ToString());
                    if (cached != null)
                    {
                        _memoryCache.Set(l1Key, cached, TimeSpan.FromMinutes(10));
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        Interlocked.Increment(ref _l2Misses);

        await _pricesSnapshotLock.WaitAsync(ct);
        try
        {
            if (_memoryCache.TryGetValue<Dictionary<int, CachedFundPriceSnapshot>>(l1Key, out var recheck))
            {
                return recheck!;
            }

            var rebuilt = await RebuildPricesSnapshotAsync(ct);
            _memoryCache.Set(l1Key, rebuilt, TimeSpan.FromMinutes(10));

            if (IsRedisAvailable())
            {
                try
                {
                    var db = _redis!.GetDatabase();
                    var json = JsonSerializer.Serialize(rebuilt);
                    await db.StringSetAsync(l2Key, json, TimeSpan.FromDays(7));
                }
                catch (Exception ex)
                {
                    RecordRedisFailure(ex);
                }
            }

            return rebuilt;
        }
        finally
        {
            _pricesSnapshotLock.Release();
        }
    }

    private async Task<Dictionary<int, CachedFundPriceSnapshot>> RebuildPricesSnapshotAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
        var funds = await fundRepo.GetAllAsync(ct);

        return funds.ToDictionary(f => f.Id, f => new CachedFundPriceSnapshot
        {
            FundId = f.Id,
            CurrentPrice = f.CurrentPrice,
            DailyChange = f.DailyChange,
            Return1M = f.Return1M,
            Return3M = f.Return3M,
            Return6M = f.Return6M,
            Return1Y = f.Return1Y,
            Return3Y = f.Return3Y,
            PricesUpdatedAt = f.PricesUpdatedAt
        });
    }

    public async Task<List<CachedPricePoint>> GetPriceHistoryAsync(int fundId, CancellationToken ct = default)
    {
        var ver = await GetDatasetVersionAsync("prices", ct);
        var l1Key = $"{_prefix}:L1:price_history:{fundId}:v{ver}";

        if (_memoryCache.TryGetValue<List<CachedPricePoint>>(l1Key, out var l1Val))
        {
            Interlocked.Increment(ref _l1Hits);
            return l1Val!;
        }

        var l2Key = $"{_prefix}:{CacheSchemaVersion}:price_history:{fundId}:v{ver}";
        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var raw = await db.StringGetAsync(l2Key);
                if (raw.HasValue)
                {
                    Interlocked.Increment(ref _l2Hits);
                    var cached = JsonSerializer.Deserialize<List<CachedPricePoint>>(raw.ToString());
                    if (cached != null)
                    {
                        _memoryCache.Set(l1Key, cached, TimeSpan.FromMinutes(10));
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFundPriceHistoryRepository>();
        var points = await repo.GetByFundIdAsync(fundId, ct);
        var dtos = points.Select(p => new CachedPricePoint
        {
            Date = p.Date,
            Price = p.Price,
            Change = p.Change,
            Performance = p.Performance
        }).ToList();

        _memoryCache.Set(l1Key, dtos, TimeSpan.FromMinutes(10));

        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var json = JsonSerializer.Serialize(dtos);
                await db.StringSetAsync(l2Key, json, TimeSpan.FromDays(7));
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        return dtos;
    }

    public async Task InvalidateCatalogAsync(CancellationToken ct = default)
    {
        Interlocked.Increment(ref _fallbackCatalogVersion);
        _memoryCache.Remove($"{_prefix}:L1:catalog_ver");

        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var key = $"{_prefix}:{CacheSchemaVersion}:version:catalog";
                await db.StringIncrementAsync(key);

                var sub = _redis.GetSubscriber();
                await sub.PublishAsync(RedisChannel.Literal($"{_prefix}:pubsub:invalidation"), "catalog");
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        // Background warm up
        _ = Task.Run(async () =>
        {
            try
            {
                await GetClassificationDatasetAsync(CancellationToken.None);
                await GetStaticFundsAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background warm up after catalog invalidation failed");
            }
        });
    }

    public async Task InvalidatePricesAsync(CancellationToken ct = default)
    {
        Interlocked.Increment(ref _fallbackPricesVersion);
        _memoryCache.Remove($"{_prefix}:L1:prices_ver");

        if (IsRedisAvailable())
        {
            try
            {
                var db = _redis!.GetDatabase();
                var key = $"{_prefix}:{CacheSchemaVersion}:version:prices";
                await db.StringIncrementAsync(key);

                var sub = _redis.GetSubscriber();
                await sub.PublishAsync(RedisChannel.Literal($"{_prefix}:pubsub:invalidation"), "prices");
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        // Background warm up
        _ = Task.Run(async () =>
        {
            try
            {
                await GetPricesSnapshotAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background warm up after prices invalidation failed");
            }
        });
    }

    public async Task ClearFundCacheAsync(CancellationToken ct = default)
    {
        if (IsRedisAvailable())
        {
            try
            {
                var endpoint = _redis!.GetEndPoints().FirstOrDefault();
                if (endpoint != null)
                {
                    var server = _redis.GetServer(endpoint);
                    var keys = server.Keys(pattern: $"{_prefix}:*").ToArray();
                    if (keys.Length > 0)
                    {
                        var db = _redis.GetDatabase();
                        await db.KeyDeleteAsync(keys);
                    }
                }
            }
            catch (Exception ex)
            {
                RecordRedisFailure(ex);
            }
        }

        await InvalidateCatalogAsync(ct);
        await InvalidatePricesAsync(ct);
    }

    public async Task RebuildAllAsync(CancellationToken ct = default)
    {
        await InvalidateCatalogAsync(ct);
        await InvalidatePricesAsync(ct);
        await GetClassificationDatasetAsync(ct);
        await GetStaticFundsAsync(ct);
        await GetPricesSnapshotAsync(ct);
    }

    public async Task<CacheStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        bool connected = IsRedisAvailable();
        long catalogVer = await GetDatasetVersionAsync("catalog", ct);
        long pricesVer = await GetDatasetVersionAsync("prices", ct);
        int keyCount = 0;
        long approxBytes = 0;

        if (connected)
        {
            try
            {
                var endpoint = _redis!.GetEndPoints().FirstOrDefault();
                if (endpoint != null)
                {
                    var server = _redis.GetServer(endpoint);
                    var keys = server.Keys(pattern: $"{_prefix}:*").ToArray();
                    keyCount = keys.Length;
                    // Approximate size: ~1.5KB per key avg estimation
                    approxBytes = keyCount * 1536L;
                }
            }
            catch
            {
            }
        }

        return new CacheStatusDto(
            RedisConnected: connected,
            CatalogVersion: catalogVer,
            PricesVersion: pricesVer,
            KeyCount: keyCount,
            ApproximateSizeBytes: approxBytes,
            L1Hits: Interlocked.Read(ref _l1Hits),
            L1Misses: Interlocked.Read(ref _l1Misses),
            L2Hits: Interlocked.Read(ref _l2Hits),
            L2Misses: Interlocked.Read(ref _l2Misses),
            LastRebuildUtc: _lastRebuildUtc,
            KeyPrefix: _prefix);
    }
}
