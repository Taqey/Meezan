using Meezan.Application.Common.Interfaces.Caching;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Features.Funds.Sync.Dtos;
using Meezan.Domain.Entities;
using Meezan.Infrastructure.Services.Funds.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Services.Funds.Sync;

public class FundSyncService : IFundSyncService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<FundSyncService> _logger;

    /// <summary>
    /// A crashed run must never block later runs: a "Running" log row is only
    /// honoured as a lock while it is younger than the lease.
    /// </summary>
    public const int RunLeaseMinutes = 30;

    public FundSyncService(IServiceProvider services, ILogger<FundSyncService> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>
    /// Expires any stale "Running" row first, then takes the lock.
    /// Returns false when another live run holds it.
    /// </summary>
    private async Task<bool> TryAcquireRunAsync(
        IFundSyncLogRepository logRepo, string triggeredBy, string mode, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var latest = await logRepo.GetLatestAsync(ct);

        if (latest != null && latest.Status == FundSyncStatus.Running)
        {
            var age = now - latest.RunAt;
            if (age < TimeSpan.FromMinutes(RunLeaseMinutes))
            {
                return false;
            }

            // Stale lease from a crashed run — release it instead of deadlocking.
            _logger.LogWarning(
                "Releasing stale fund sync lock (run {RunId}, started {RunAt:o}, age {Minutes} min)",
                latest.Id, latest.RunAt, age.TotalMinutes);

            latest.Status = FundSyncStatus.Failed;
            latest.FinishedAt = now;
            latest.DurationSeconds = (int)age.TotalSeconds;
            latest.ErrorSummary = "Stale run lock released: previous run exceeded the "
                + RunLeaseMinutes + " minute lease (crashed or killed).";
            logRepo.Update(latest);
        }

        var log = new FundSyncLog
        {
            Status = FundSyncStatus.Running,
            TriggeredBy = triggeredBy,
            Mode = mode,
            RunAt = now
        };
        await logRepo.AddAsync(log, ct);
        return true;
    }

    public async Task<FundSyncLog> RunSyncAsync(string triggeredBy, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<IFundSyncSettingsRepository>();
        var settings = await settingsRepo.GetAsync(cancellationToken);

        if (settings == null || !settings.IsEnabled)
        {
            throw new InvalidOperationException("Fund sync is disabled");
        }

        var logRepo = scope.ServiceProvider.GetRequiredService<IFundSyncLogRepository>();

        // Overlap guard: lease-based so a crashed run cannot block future runs.
        if (settings.PreventOverlap && !await TryAcquireRunAsync(logRepo, triggeredBy, "Full", cancellationToken))
        {
            throw new InvalidOperationException(
                $"Another sync run is already in progress (lease {RunLeaseMinutes} min).");
        }

        var syncLog = logRepo.LastAdded!;
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        var startTime = DateTime.UtcNow;
        int updatedFunds = 0;
        int failedFunds = 0;
        int skippedFunds = 0;
        var details = new List<FundSyncLogDetail>();

        try
        {
            // Step 1: Fetch fund list
            var sndukClient = scope.ServiceProvider.GetRequiredService<ISndukFundClient>();
            var fundListResponse = await sndukClient.GetFundListAsync(cancellationToken);

            var sndukFunds = (fundListResponse?.Funds ?? [])
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .ToArray();
            if (sndukFunds.Length == 0)
            {
                throw new InvalidOperationException("No funds returned from SNDUK");
            }

            syncLog.TotalFunds = sndukFunds.Length;
            _logger.LogInformation("Fetched {Count} funds from SNDUK", sndukFunds.Length);

            // Step 2: Upsert funds
            var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
            var priceRepo = scope.ServiceProvider.GetRequiredService<IFundPriceHistoryRepository>();
            var analysisRepo = scope.ServiceProvider.GetRequiredService<IFundAnalysisRepository>();
            var similarRepo = scope.ServiceProvider.GetRequiredService<IFundSimilarRepository>();
            var categoryRepo = scope.ServiceProvider.GetRequiredService<IFundCategoryRepository>();
            var overrideRepo = scope.ServiceProvider.GetRequiredService<IFundCategoryOverrideRepository>();
            var detailRepo = scope.ServiceProvider.GetRequiredService<IFundSyncLogDetailRepository>();

            // Ensure the baseline auto-derived categories exist (never recreates
            // deleted ones, never renames existing ones).
            await EnsureDefaultCategoriesAsync(categoryRepo, cancellationToken);
            await fundRepo.SaveChangesAsync(cancellationToken);

            foreach (var sndukFund in sndukFunds)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var detail = new FundSyncLogDetail
                {
                    FundSyncLogId = syncLog.Id,
                    FundCode = sndukFund.Code,
                    AttemptedAt = DateTime.UtcNow
                };

                try
                {
                    var fund = await UpsertFundAsync(fundRepo, sndukFund, cancellationToken);
                    detail.FundId = fund.Id;

                    // Download logo if needed
                    if (!string.IsNullOrWhiteSpace(sndukFund.LogoUrl) && string.IsNullOrWhiteSpace(fund.LocalLogoPath))
                    {
                        await DownloadLogoAsync(fund, sndukFund.LogoUrl!, cancellationToken);
                        fundRepo.Update(fund);
                    }

                    // Step 3: Fetch and store price history
                    await SyncPriceHistoryAsync(fund, priceRepo, sndukClient, settings.RequestDelaySeconds, cancellationToken);

                    // Step 4: Fetch and store analysis
                    await SyncAnalysisAsync(fund, analysisRepo, sndukClient, settings.RequestDelaySeconds, cancellationToken);

                    // Step 5: Fetch and store similar funds
                    await SyncSimilarFundsAsync(fund, similarRepo, fundRepo, sndukClient, settings.RequestDelaySeconds, cancellationToken);

                    // Step 6: Auto-derive categories
                    await AutoDeriveCategoriesAsync(fund, categoryRepo, overrideRepo, cancellationToken);

                    // Step 7: Classification tree (manual assignments preserved; auto recalculated)
                    try
                    {
                        var classification = scope.ServiceProvider.GetRequiredService<IFundClassificationService>();
                        await classification.SyncFundClassificationAsync(fund.Id, cancellationToken);
                    }
                    catch (Exception cex)
                    {
                        _logger.LogWarning(cex, "Classification sync skipped for fund {Code}", sndukFund.Code);
                    }

                    updatedFunds++;
                    detail.Status = FundSyncDetailStatus.Success;
                    _logger.LogDebug("Successfully synced fund {Code}", sndukFund.Code);
                }
                catch (Exception ex)
                {
                    failedFunds++;
                    detail.Status = FundSyncDetailStatus.Failed;
                    detail.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Failed to sync fund {Code}", sndukFund.Code);
                }
                finally
                {
                    details.Add(detail);

                    // Delay between requests
                    if (settings.RequestDelaySeconds > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(settings.RequestDelaySeconds), cancellationToken);
                    }
                }
            }

            // Update categories based on auto-rules (for new categories created by auto-derivation)
            await RefreshAutoCategoriesAsync(categoryRepo, fundRepo, overrideRepo, cancellationToken);

            // Save all details
            if (details.Any())
            {
                await detailRepo.AddRangeAsync(details, cancellationToken);
            }

            syncLog.UpdatedFunds = updatedFunds;
            syncLog.FailedFunds = failedFunds;
            syncLog.SkippedFunds = skippedFunds;
            syncLog.Status = failedFunds > 0 ? FundSyncStatus.Partial : FundSyncStatus.Success;
            syncLog.FinishedAt = DateTime.UtcNow;
            syncLog.DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds;

            if (failedFunds > 0)
            {
                syncLog.ErrorSummary = $"{failedFunds} fund(s) failed to sync";
            }

            logRepo.Update(syncLog);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Fund sync completed: {Updated} updated, {Failed} failed, duration: {Duration}s",
                updatedFunds, failedFunds, syncLog.DurationSeconds);

            return syncLog;
        }
        catch (Exception ex)
        {
            syncLog.Status = FundSyncStatus.Failed;
            syncLog.FinishedAt = DateTime.UtcNow;
            syncLog.DurationSeconds = (int)(DateTime.UtcNow - startTime).TotalSeconds;
            syncLog.ErrorSummary = ex.Message;

            logRepo.Update(syncLog);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

            _logger.LogError(ex, "Fund sync failed");
            throw;
        }
    }

    private async Task<Fund> UpsertFundAsync(IFundRepository fundRepo, SndukFundDto dto, CancellationToken cancellationToken)
    {
        var existing = await fundRepo.GetBySndukIdAsync(dto.Id, cancellationToken);

        var fund = existing ?? new Fund
        {
            SndukId = dto.Id,
            CreatedAt = DateTime.UtcNow
        };

        // Update fields
        fund.PublicId = dto.PublicId;
        fund.Slug = dto.CustomSlug ?? dto.Slug ?? string.Empty;
        fund.NameAr = dto.Name;
        fund.NameEn = dto.NameEn ?? string.Empty;
        fund.Code = dto.Code;
        fund.Type = ParseFundType(dto.Type);
        fund.TypeNameAr = dto.TypeNameAr ?? string.Empty;
        fund.TypeNameEn = dto.TypeNameEn ?? string.Empty;
        fund.RiskLevel = ParseRiskLevel(dto.RiskLevel);
        fund.IsIslamic = dto.IsIslamic ?? false;
        fund.IsFeatured = dto.IsFeatured ?? false;
        fund.IsActive = dto.IsActive ?? true;
        fund.CurrentPrice = ParseDecimal(dto.CurrentPrice);
        fund.DailyChange = ParseDecimal(dto.DailyChange);
        fund.Return1M = ParseDecimal(dto.Returns?.M1);
        fund.Return3M = ParseDecimal(dto.Returns?.M3);
        fund.Return6M = ParseDecimal(dto.Returns?.M6);
        fund.Return1Y = ParseDecimal(dto.Returns?.Y1);
        fund.Return3Y = ParseDecimal(dto.Returns?.Y3);
        fund.TotalScore = ParseDecimal(dto.TotalScore);
        fund.Aum = ParseDecimal(dto.Aum);
        fund.ManagementFee = ParseDecimal(dto.ManagementFee);
        fund.MinimumInvestment = dto.MinimumInvestment ?? string.Empty;
        fund.Currency = dto.Currency ?? "EGP";
        fund.AssetManagerId = dto.AssetManagerId;
        fund.AssetManagerName = dto.AssetManagerName ?? string.Empty;
        fund.AssetManagerNameEn = dto.AssetManagerNameEn ?? string.Empty;
        fund.AssetManagerCode = dto.AssetManagerCode ?? string.Empty;
        fund.LogoUrl = dto.LogoUrl ?? string.Empty;
        fund.DescriptionAr = dto.Description ?? string.Empty;
        fund.DescriptionEn = dto.DescriptionEn ?? string.Empty;
        fund.InvestmentStrategy = dto.InvestmentStrategy ?? string.Empty;
        fund.InceptionDate = ParseDateTime(dto.InceptionDate);
        fund.ProspectusUrl = dto.ProspectusUrl ?? string.Empty;
        fund.SndukCreatedAt = ParseDateTime(dto.CreatedAt) ?? DateTime.UtcNow;
        fund.SndukUpdatedAt = ParseDateTime(dto.UpdatedAt) ?? DateTime.UtcNow;
        fund.LastSyncedAt = DateTime.UtcNow;
        fund.UpdatedAt = DateTime.UtcNow;

        if (existing == null)
        {
            await fundRepo.AddAsync(fund, cancellationToken);
        }
        else
        {
            fundRepo.Update(fund);
        }

        await fundRepo.SaveChangesAsync(cancellationToken);
        return fund;
    }

    private async Task SyncPriceHistoryAsync(Fund fund, IFundPriceHistoryRepository priceRepo, ISndukFundClient sndukClient, int delaySeconds, CancellationToken cancellationToken)
    {
        // Try to get longest available history first
        var timeframes = new[] { "3Y", "1Y", "6M", "3M", "1M" };

        for (var i = 0; i < timeframes.Length; i++)
        {
            var timeframe = timeframes[i];
            var response = await sndukClient.GetFundPerformanceAsync(fund.SndukId, timeframe, cancellationToken);

            if (response != null)
            {
                var data = response.Data ?? [];
                if (data.Length > 0)
                {
                    var maxDate = await priceRepo.GetMaxDateAsync(fund.Id, cancellationToken);
                    var newPoints = new List<FundPriceHistory>();

                    foreach (var point in data)
                    {
                        if (!DateTime.TryParse(point.Date, out var date)) continue;
                        if (!decimal.TryParse(point.Price, out var price)) continue;
                        if (price <= 0) continue;
                        if (maxDate.HasValue && date <= maxDate.Value) continue;

                        newPoints.Add(new FundPriceHistory
                        {
                            FundId = fund.Id,
                            Date = date,
                            Price = price,
                            Change = ParseDecimal(point.Change),
                            Performance = ParseDecimal(point.Performance),
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (newPoints.Any())
                    {
                        await priceRepo.AddRangeAsync(newPoints, cancellationToken);
                    }

                    // Also store summary if available
                    var summary = response.Summary;
                    if (summary != null)
                    {
                        // Could store summary in a separate table or update fund
                    }

                    break; // Successfully fetched one timeframe, no need to try others
                }
            }

            // Delay between timeframe attempts
            if (i < timeframes.Length - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }
    }

    private async Task SyncAnalysisAsync(Fund fund, IFundAnalysisRepository analysisRepo, ISndukFundClient sndukClient, int delaySeconds, CancellationToken cancellationToken)
    {
        var response = await sndukClient.GetFundAnalysisAsync(fund.SndukId, cancellationToken);

        if (response != null)
        {
            var existing = await analysisRepo.GetByFundIdAsync(fund.Id, cancellationToken);
            var analysis = existing ?? new FundAnalysis { FundId = fund.Id, CreatedAt = DateTime.UtcNow };

            var scores = response.Scores;
            var metrics = response.RawMetrics;
            var explanations = response.Explanations;

            // Scores: -1 means not available, store as null
            analysis.PerformanceScore = NormalizeScore(scores?.Performance);
            analysis.RiskScore = NormalizeScore(scores?.Risk);
            analysis.CostScore = NormalizeScore(scores?.Cost);
            analysis.StabilityScore = NormalizeScore(scores?.Stability);
            analysis.DiversificationScore = NormalizeScore(scores?.Diversification);
            analysis.TotalScore = NormalizeScore(scores?.Total);

            analysis.Cagr = metrics?.Cagr;
            analysis.Alpha = metrics?.Alpha;
            analysis.Volatility = metrics?.Volatility;
            analysis.Consistency = metrics?.Consistency;
            analysis.MaxDrawdown = metrics?.MaxDrawdown;
            analysis.SharpeRatio = metrics?.SharpeRatio;

            analysis.WeightsJson = response.Weights?.GetRawText() ?? string.Empty;

            analysis.PerformanceExplanationAr = string.Join(" ", explanations?.Performance?.Ar ?? []);
            analysis.PerformanceExplanationEn = string.Join(" ", explanations?.Performance?.En ?? []);
            analysis.RiskExplanationAr = string.Join(" ", explanations?.Risk?.Ar ?? []);
            analysis.RiskExplanationEn = string.Join(" ", explanations?.Risk?.En ?? []);
            analysis.CostExplanationAr = string.Join(" ", explanations?.Cost?.Ar ?? []);
            analysis.CostExplanationEn = string.Join(" ", explanations?.Cost?.En ?? []);
            analysis.StabilityExplanationAr = string.Join(" ", explanations?.Stability?.Ar ?? []);
            analysis.StabilityExplanationEn = string.Join(" ", explanations?.Stability?.En ?? []);
            analysis.DiversificationExplanationAr = string.Join(" ", explanations?.Diversification?.Ar ?? []);
            analysis.DiversificationExplanationEn = string.Join(" ", explanations?.Diversification?.En ?? []);

            if (DateTime.TryParse(response.CalculatedAt, out var calcAt))
                analysis.CalculatedAt = calcAt;
            else
                analysis.CalculatedAt = DateTime.UtcNow;

            analysis.UpdatedAt = DateTime.UtcNow;

            if (existing == null)
                await analysisRepo.AddAsync(analysis, cancellationToken);
            else
                analysisRepo.Update(analysis);
        }
    }

    private async Task SyncSimilarFundsAsync(Fund fund, IFundSimilarRepository similarRepo, IFundRepository fundRepo, ISndukFundClient sndukClient, int delaySeconds, CancellationToken cancellationToken)
    {
        var response = await sndukClient.GetFundCompetingAsync(fund.SndukId, cancellationToken);

        if (response != null)
        {
            var competingFunds = response.Funds ?? [];

            // Clear existing similar funds
            similarRepo.ClearByFundId(fund.Id);

            // Add new similar funds
            var similars = new List<FundSimilar>();
            foreach (var competing in competingFunds)
            {
                // Reuse the same-scope fundRepo — avoids creating unmanaged IServiceScope instances
                var similarFund = await fundRepo.GetBySndukIdAsync(competing.Id, cancellationToken);
                if (similarFund != null)
                {
                    similars.Add(new FundSimilar
                    {
                        FundId = fund.Id,
                        SimilarFundId = similarFund.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (similars.Any())
            {
                await similarRepo.AddRangeAsync(similars, cancellationToken);
            }
        }
    }


    private async Task EnsureDefaultCategoriesAsync(
        IFundCategoryRepository categoryRepo,
        CancellationToken cancellationToken)
    {
        var all = await categoryRepo.GetAllAsync(true, cancellationToken);
        var bySlug = all.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var added = 0;
        foreach (var def in FundAutoClassification.Defaults)
        {
            // Never recreate a deleted category and never rename an existing one:
            // the slug is the stable identity. A renamed category keeps its slug.
            if (bySlug.ContainsKey(def.Slug))
                continue;
            var category = new FundCategory
            {
                NameAr = def.NameAr,
                NameEn = def.NameEn,
                Slug = def.Slug,
                Icon = string.Empty,
                Color = string.Empty,
                IsAutoDerived = true,
                AutoRuleJson = def.AutoRuleJson,
                IsVisible = true,
                DisplayOrder = def.DisplayOrder,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "AutoSync",
                UpdatedBy = "AutoSync"
            };
            await categoryRepo.AddAsync(category, cancellationToken);
            // Provenance is recorded on the row itself (CreatedBy = "AutoSync").
            // FundCategoryChangeLog requires a real fund row, so category-level
            // creation is not logged there.
            bySlug[def.Slug] = category;
            added++;
        }
        if (added > 0)
            _logger.LogInformation("Seeded {Count} baseline fund categories", added);
    }

    private async Task AutoDeriveCategoriesAsync(Fund fund, IFundCategoryRepository categoryRepo, IFundCategoryOverrideRepository overrideRepo, CancellationToken cancellationToken)
    {
        var categories = await categoryRepo.GetAllAsync(true, cancellationToken);
        var existingOverrides = await overrideRepo.GetByFundIdAsync(fund.Id, cancellationToken);

        // Only rows created by a human count as manual; rows created by AutoSync
        // are the recalculable auto-derived part and may be refreshed.
        var manualIncludes = existingOverrides.Where(o => o.Action == OverrideAction.Include && o.CreatedBy != "AutoSync").Select(o => o.CategoryId).ToHashSet();
        var manualExcludes = existingOverrides.Where(o => o.Action == OverrideAction.Exclude && o.CreatedBy != "AutoSync").Select(o => o.CategoryId).ToHashSet();

        foreach (var category in categories.Where(c => c.IsVisible && !c.IsDeleted && c.IsAutoDerived))
        {
            bool shouldInclude = false;
            string reason = string.Empty;

            if (manualExcludes.Contains(category.Id))
            {
                // Manual exclude wins - don't include
                continue;
            }

            if (manualIncludes.Contains(category.Id))
            {
                // Manual include wins - include
                shouldInclude = true;
                reason = "Manual include override";
            }
            else
            {
                // Auto-derivation logic (shared evaluator; reason recomputable on demand)
                shouldInclude = FundAutoClassification.Evaluate(fund, category.AutoRuleJson, out reason);
            }

            // Create or remove override based on evaluation
            var existingOverride = existingOverrides.FirstOrDefault(o => o.CategoryId == category.Id);

            if (shouldInclude && existingOverride == null)
            {
                await overrideRepo.AddAsync(new FundCategoryOverride
                {
                    FundId = fund.Id,
                    CategoryId = category.Id,
                    Action = OverrideAction.Include,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "AutoSync"
                }, cancellationToken);
            }
            else if (!shouldInclude && existingOverride != null
                && existingOverride.Action == OverrideAction.Include
                && existingOverride.CreatedBy == "AutoSync")
            {
                // Remove stale auto-include if no longer matches; manual rows are never deleted here
                overrideRepo.Delete(existingOverride);
            }
        }
    }

    private async Task RefreshAutoCategoriesAsync(IFundCategoryRepository categoryRepo, IFundRepository fundRepo, IFundCategoryOverrideRepository overrideRepo, CancellationToken cancellationToken)
    {
        // This would refresh categories based on auto-rules
        // For now, just ensure deleted auto-categories stay deleted
        var deletedAutoCategories = await categoryRepo.GetAllAsync(true, cancellationToken)
            .ContinueWith(t => t.Result.Where(c => c.IsDeleted && c.IsAutoDerived).ToList(), cancellationToken);

        // The sync job should NOT recreate deleted auto-categories
        // They remain deleted unless manually restored
    }

    private static FundType ParseFundType(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "balanced" => FundType.Balanced,
            "capital_preservation" => FundType.CapitalPreservation,
            "commodities" => FundType.Commodities,
            "equity" => FundType.Equity,
            "fixed_income" => FundType.FixedIncome,
            "fixed_income_usd" => FundType.FixedIncomeUsd,
            "gold" => FundType.Gold,
            "index" => FundType.Index,
            "money_market" => FundType.MoneyMarket,
            _ => FundType.Unknown
        };
    }

    private static FundRiskLevel ParseRiskLevel(string? risk)
    {
        return risk?.ToLowerInvariant() switch
        {
            "low" => FundRiskLevel.Low,
            "medium" => FundRiskLevel.Medium,
            "high" => FundRiskLevel.High,
            _ => FundRiskLevel.Unknown
        };
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, out var result)) return result;
        return null;
    }

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, out var result)) return result;
        return null;
    }

    private static decimal? NormalizeScore(decimal? score)
    {
        if (!score.HasValue || score.Value < 0) return null; // -1 means not available
        return score;
    }

    private async Task DownloadLogoAsync(Fund fund, string logoUrl, CancellationToken cancellationToken)
    {
        // LocalLogoPath is ONLY written when a file actually landed on disk; a
        // disabled/failed storage leaves it empty so the UI falls back to LogoUrl.
        var storage = _services.GetService<IFundLogoStorage>();
        if (storage == null) return;

        var stored = await storage.StoreAsync(logoUrl, fund.Slug, cancellationToken);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            fund.LocalLogoPath = stored;
        }
    }

    /// <summary>
    /// One-off backfill for logos stored before the downloader existed. Safe to
    /// re-run: funds whose file already exists on disk are skipped.
    /// </summary>
    public async Task<int> BackfillLogosAsync(CancellationToken cancellationToken = default)
    {
        var storage = _services.GetService<IFundLogoStorage>();
        if (storage == null)
        {
            _logger.LogWarning("Logo backfill skipped: IFundLogoStorage is not registered (RootPath not configured).");
            return 0;
        }

        using var scope = _services.CreateScope();
        var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var funds = await fundRepo.GetAllAsync(cancellationToken);
        var saved = 0;
        foreach (var fund in funds)
        {
            if (string.IsNullOrWhiteSpace(fund.LogoUrl)) continue;
            if (!string.IsNullOrWhiteSpace(fund.LocalLogoPath) && LogoExists(fund.LocalLogoPath)) continue;

            await DownloadLogoAsync(fund, fund.LogoUrl, cancellationToken);
            if (!string.IsNullOrWhiteSpace(fund.LocalLogoPath))
            {
                fundRepo.Update(fund);
                saved++;
            }
        }

        if (saved > 0) await uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Logo backfill stored {Count} file(s).", saved);
        return saved;
    }

    private static bool LogoExists(string relativePath)
    {
        var root = Environment.GetEnvironmentVariable("MEEZAN_LOGO_ROOT");
        if (string.IsNullOrWhiteSpace(root)) return false;
        try
        {
            return File.Exists(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
        catch { return false; }
    }

    public async Task<FundSyncLog?> GetLatestRunAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var logRepo = scope.ServiceProvider.GetRequiredService<IFundSyncLogRepository>();
        return await logRepo.GetLatestAsync(cancellationToken);
    }

    public async Task<FundPriceSyncResultDto> RunPriceSyncAsync(string triggeredBy, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<IFundSyncSettingsRepository>();
        var settings = await settingsRepo.GetAsync(cancellationToken);

        if (settings == null || !settings.IsEnabled)
        {
            throw new InvalidOperationException("Fund sync is disabled");
        }

        var logRepo = scope.ServiceProvider.GetRequiredService<IFundSyncLogRepository>();

        if (settings.PreventOverlap && !await TryAcquireRunAsync(logRepo, triggeredBy, "Prices", cancellationToken))
        {
            throw new InvalidOperationException(
                $"Another sync run is already in progress (lease {RunLeaseMinutes} min).");
        }

        var syncLog = logRepo.LastAdded!;
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int updatedFunds = 0;
        int failedFunds = 0;
        int newFundsDetected = 0;
        int missingFundsReported = 0;
        var details = new List<FundSyncLogDetail>();

        try
        {
            var sndukClient = scope.ServiceProvider.GetRequiredService<ISndukFundClient>();
            var fundListResponse = await sndukClient.GetFundListAsync(cancellationToken);

            var sndukFunds = (fundListResponse?.Funds ?? [])
                .GroupBy(f => f.Id)
                .Select(g => g.First())
                .ToArray();

            if (sndukFunds.Length == 0)
            {
                throw new InvalidOperationException("No funds returned from SNDUK");
            }

            syncLog.TotalFunds = sndukFunds.Length;
            _logger.LogInformation("Price sync: Fetched {Count} funds from SNDUK", sndukFunds.Length);

            var fundRepo = scope.ServiceProvider.GetRequiredService<IFundRepository>();
            var priceRepo = scope.ServiceProvider.GetRequiredService<IFundPriceHistoryRepository>();
            var detailRepo = scope.ServiceProvider.GetRequiredService<IFundSyncLogDetailRepository>();
            var allDbFunds = await fundRepo.GetAllAsync(cancellationToken);
            var dbFundsBySndukId = allDbFunds.ToDictionary(f => f.SndukId);
            var dbFundsByCode = allDbFunds.ToDictionary(f => f.Code.Trim().ToUpperInvariant());
            var matchedSndukIds = new HashSet<long>();

            var tz = Meezan.Infrastructure.Services.BackgroundServices.FundSyncBackgroundService.ResolveCairoTimeZone();
            var nowCairoDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

            foreach (var sndukFund in sndukFunds)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Match with our DB
                Fund? existingFund = null;
                if (dbFundsBySndukId.TryGetValue(sndukFund.Id, out var fById))
                {
                    existingFund = fById;
                }
                else if (!string.IsNullOrWhiteSpace(sndukFund.Code) && dbFundsByCode.TryGetValue(sndukFund.Code.Trim().ToUpperInvariant(), out var fByCode))
                {
                    existingFund = fByCode;
                }

                if (existingFund == null)
                {
                    newFundsDetected++;
                    continue;
                }

                matchedSndukIds.Add(existingFund.SndukId);

                var detail = new FundSyncLogDetail
                {
                    FundSyncLogId = syncLog.Id,
                    FundId = existingFund.Id,
                    FundCode = existingFund.Code,
                    AttemptedAt = DateTime.UtcNow
                };

                try
                {
                    var price = ParseDecimal(sndukFund.CurrentPrice);
                    if (price.HasValue && price.Value > 0)
                    {
                        existingFund.CurrentPrice = price.Value;
                    }
                    if (sndukFund.DailyChange != null)
                    {
                        existingFund.DailyChange = ParseDecimal(sndukFund.DailyChange);
                    }
                    if (sndukFund.Returns != null)
                    {
                        existingFund.Return1M = ParseDecimal(sndukFund.Returns.M1);
                        existingFund.Return3M = ParseDecimal(sndukFund.Returns.M3);
                        existingFund.Return6M = ParseDecimal(sndukFund.Returns.M6);
                        existingFund.Return1Y = ParseDecimal(sndukFund.Returns.Y1);
                        existingFund.Return3Y = ParseDecimal(sndukFund.Returns.Y3);
                    }

                    existingFund.PricesUpdatedAt = DateTime.UtcNow;
                    fundRepo.Update(existingFund);

                    // Upsert today's point in FundPriceHistory
                    if (price.HasValue && price.Value > 0)
                    {
                        var pointDate = ParseDateTime(sndukFund.UpdatedAt)?.Date ?? nowCairoDate;
                        var existingPoint = await priceRepo.GetByFundIdAndDateAsync(existingFund.Id, pointDate, cancellationToken);
                        if (existingPoint == null)
                        {
                            await priceRepo.AddRangeAsync(new[]
                            {
                                new FundPriceHistory
                                {
                                    FundId = existingFund.Id,
                                    Date = pointDate,
                                    Price = price.Value,
                                    Change = existingFund.DailyChange,
                                    CreatedAt = DateTime.UtcNow
                                }
                            }, cancellationToken);
                        }
                        else
                        {
                            existingPoint.Price = price.Value;
                            existingPoint.Change = existingFund.DailyChange;
                            priceRepo.UpdateRange(new[] { existingPoint });
                        }
                    }

                    updatedFunds++;
                    detail.Status = FundSyncDetailStatus.Success;
                }
                catch (Exception ex)
                {
                    failedFunds++;
                    detail.Status = FundSyncDetailStatus.Failed;
                    detail.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Price sync: Failed to update price for fund {Code}", sndukFund.Code);
                }
                finally
                {
                    details.Add(detail);
                }
            }

            missingFundsReported = allDbFunds.Count(f => !matchedSndukIds.Contains(f.SndukId));

            if (details.Any())
            {
                await detailRepo.AddRangeAsync(details, cancellationToken);
            }

            stopwatch.Stop();
            var durationSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 2);

            syncLog.UpdatedFunds = updatedFunds;
            syncLog.FailedFunds = failedFunds;
            syncLog.SkippedFunds = newFundsDetected;
            syncLog.Status = failedFunds > 0 ? FundSyncStatus.Partial : FundSyncStatus.Success;
            syncLog.FinishedAt = DateTime.UtcNow;
            syncLog.DurationSeconds = (int)stopwatch.Elapsed.TotalSeconds;

            var summaryParts = new List<string>();
            if (failedFunds > 0) summaryParts.Add($"{failedFunds} fund(s) failed");
            if (newFundsDetected > 0) summaryParts.Add($"{newFundsDetected} new funds detected, run a full sync");
            if (missingFundsReported > 0) summaryParts.Add($"{missingFundsReported} DB fund(s) missing from SNDUK list");
            syncLog.ErrorSummary = summaryParts.Count > 0 ? string.Join("; ", summaryParts) : null;

            logRepo.Update(syncLog);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

            // Task 3 Hook: Invalidate Price Cache
            var priceCacheInvalidator = scope.ServiceProvider.GetService<IFundPriceCacheInvalidator>();
            if (priceCacheInvalidator != null)
            {
                await priceCacheInvalidator.InvalidatePriceCacheAsync(cancellationToken);
            }

            _logger.LogInformation("Fund price sync completed: {Updated} updated, {Failed} failed, {New} new, {Missing} missing, duration: {Duration}s",
                updatedFunds, failedFunds, newFundsDetected, missingFundsReported, durationSeconds);

            return new FundPriceSyncResultDto(
                TotalSndukFunds: sndukFunds.Length,
                UpdatedFunds: updatedFunds,
                MissingFromSndukCount: missingFundsReported,
                NewInSndukCount: newFundsDetected,
                DurationSeconds: durationSeconds,
                SyncedAtUtc: DateTime.UtcNow,
                Message: syncLog.ErrorSummary);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            syncLog.Status = FundSyncStatus.Failed;
            syncLog.FinishedAt = DateTime.UtcNow;
            syncLog.DurationSeconds = (int)stopwatch.Elapsed.TotalSeconds;
            syncLog.ErrorSummary = ex.Message;

            logRepo.Update(syncLog);
            try
            {
                await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
            }
            catch { }

            _logger.LogError(ex, "Fund price sync job failed");
            throw;
        }
    }

    public async Task<(List<FundSyncLog> Items, int TotalCount)> GetRunHistoryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var logRepo = scope.ServiceProvider.GetRequiredService<IFundSyncLogRepository>();
        return await logRepo.QueryPagedAsync(page, pageSize, cancellationToken);
    }
}
