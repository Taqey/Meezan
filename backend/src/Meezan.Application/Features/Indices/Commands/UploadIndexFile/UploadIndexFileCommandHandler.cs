using Meezan.Application.Common;
using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Meezan.Application.Features.Indices.Commands.UploadIndexFile;

public class UploadIndexFileCommandHandler : IRequestHandler<UploadIndexFileCommand, UploadIndexFileResultDto>
{
    private readonly IIndexRepository _indexRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IUploadHistoryRepository _uploadHistoryRepository;
    private readonly IExcelParserService _excelParserService;
    private readonly IShariahComplianceRepository _complianceRepository;
    private readonly IStockShariahMetricsRepository _metricsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UploadIndexFileCommand> _validator;

    public UploadIndexFileCommandHandler(
        IIndexRepository indexRepository,
        IStockRepository stockRepository,
        IUploadHistoryRepository uploadHistoryRepository,
        IExcelParserService excelParserService,
        IShariahComplianceRepository complianceRepository,
        IStockShariahMetricsRepository metricsRepository,
        IUnitOfWork unitOfWork,
        IValidator<UploadIndexFileCommand> validator)
    {
        _indexRepository = indexRepository;
        _stockRepository = stockRepository;
        _uploadHistoryRepository = uploadHistoryRepository;
        _excelParserService = excelParserService;
        _complianceRepository = complianceRepository;
        _metricsRepository = metricsRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<UploadIndexFileResultDto> Handle(UploadIndexFileCommand request, CancellationToken cancellationToken)
    {
        // 1. FluentValidation check
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Validate Index exists
        var index = await _indexRepository.GetByCodeAsync(request.IndexCode, cancellationToken);
        if (index == null)
        {
            throw new KeyNotFoundException($"Index with code '{request.IndexCode}' was not found.");
        }

        // 3. Parse Excel
        var parsedConstituents = await _excelParserService.ParseIndexConstituentsAsync(
            request.FileStream,
            request.FileName,
            cancellationToken);

        var result = new UploadIndexFileResultDto
        {
            IndexCode = index.Code
        };

        if (parsedConstituents.Count == 0)
        {
            result.Status = "Failed";
            result.Message = "No valid constituent rows were found in the uploaded file.";
            return result;
        }

        bool isSectoralUpload = string.Equals(index.Code, "Sectoral-Indices", StringComparison.OrdinalIgnoreCase);

        // Validate weight column sums and warn if they don't add up to ~100% (skip for Sectoral-Indices, where weights are never stored)
        var weightedItems = parsedConstituents.Where(c => c.Weight.HasValue && c.Weight.Value > 0).ToList();
        if (!isSectoralUpload && weightedItems.Count > 0)
        {
            var sectors = parsedConstituents
                .Select(c => c.SectorEn ?? c.SectorAr)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (sectors.Count > 1)
            {
                var groupAnomalies = new List<string>();
                var groups = parsedConstituents.GroupBy(c => (c.SectorEn ?? c.SectorAr ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);
                foreach (var g in groups)
                {
                    var gWeights = g.Where(c => c.Weight.HasValue && c.Weight.Value > 0).ToList();
                    if (gWeights.Count == 0) continue;
                    var sum = gWeights.Sum(c => c.Weight!.Value);
                    if (sum < 99.0m || sum > 101.0m)
                    {
                        groupAnomalies.Add($"{g.Key}: {sum:F2}%");
                    }
                }
                if (groupAnomalies.Count > 0)
                {
                    result.Warning = $"تنبيه: مجموع أوزان بعض القطاعات لا يساوي 100% ({string.Join(", ", groupAnomalies)})";
                }
            }
            else
            {
                var totalSum = weightedItems.Sum(c => c.Weight!.Value);
                if (totalSum < 99.0m || totalSum > 101.0m)
                {
                    result.Warning = $"تنبيه: إجمالي أوزان المؤشر لا يساوي 100% (المجموع الفعلي: {totalSum:F2}%)";
                }
            }
        }

        // 4. Pre-load all existing stocks for in-memory Arabic-name matching (avoids N+1).
        //    Stocks with no Reuters code / ISIN in the file can only be found by name.
        var allExistingStocks = await _stockRepository.GetAllIncludingInactiveAsync(cancellationToken);
        // Build normalised-name → Stock lookup (first match wins for duplicates).
        var byNormalizedAr = new Dictionary<string, Stock>(StringComparer.Ordinal);
        foreach (var s in allExistingStocks)
        {
            var key = ArabicTextNormalizer.Normalize(s.NameAr);
            if (!string.IsNullOrEmpty(key) && !byNormalizedAr.ContainsKey(key))
                byNormalizedAr[key] = s;
        }

        // 5. Pre-resolve and persist all distinct sectors first so they receive valid DB identity IDs (> 0)
        // before any Stock rows are inserted or updated, preventing FK constraint conflicts.
        var sectorMap = new Dictionary<string, Sector>(StringComparer.OrdinalIgnoreCase);
        int sectorsAdded = 0;

        foreach (var item in parsedConstituents)
        {
            var cleanAr = item.SectorAr?.Trim();
            var cleanEn = item.SectorEn?.Trim();
            if (string.IsNullOrWhiteSpace(cleanAr) && string.IsNullOrWhiteSpace(cleanEn))
                continue;

            var key = !string.IsNullOrEmpty(cleanEn) ? cleanEn : cleanAr!;
            if (!sectorMap.ContainsKey(key))
            {
                var (sec, isNew) = await _stockRepository.GetOrCreateSectorWithFlagAsync(cleanAr, cleanEn, cancellationToken);
                if (sec != null)
                {
                    sectorMap[key] = sec;
                    if (!string.IsNullOrEmpty(cleanAr) && !sectorMap.ContainsKey(cleanAr))
                        sectorMap[cleanAr] = sec;
                    if (!string.IsNullOrEmpty(cleanEn) && !sectorMap.ContainsKey(cleanEn))
                        sectorMap[cleanEn] = sec;

                    if (isNew)
                    {
                        sectorsAdded++;
                    }
                }
            }
        }

        // 6. Begin transaction for steps: upsert stocks/sectors, replace constituents, update index, log history
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // If any new sectors were added, save changes now to guarantee they have generated DB IDs
            if (sectorsAdded > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var constituentsToSave = new List<IndexConstituent>();
            int insertedStocks  = 0;
            int updatedStocks   = 0;
            int skippedStocks   = 0;

            // Track in-session newly created stocks by their normalized Arabic name so that
            // if the same Arabic name appears twice in the file we don't create two rows.
            var newStocksByNormalizedAr = new Dictionary<string, Stock>(StringComparer.Ordinal);

            foreach (var item in parsedConstituents)
            {
                // Compute ticker: ReutersCode minus exchange suffix (e.g. .CA), else SymbolCode
                var ticker = !string.IsNullOrWhiteSpace(item.Ticker)
                    ? item.Ticker.Trim().ToUpperInvariant()
                    : ComputeTicker(item.ReutersCode, item.SymbolCode);

                // ── Sector resolution from pre-resolved cache ───────────────────
                Sector? sector = null;
                var cleanSecEn = item.SectorEn?.Trim();
                var cleanSecAr = item.SectorAr?.Trim();
                if (!string.IsNullOrEmpty(cleanSecEn) && sectorMap.TryGetValue(cleanSecEn, out var sByEn))
                {
                    sector = sByEn;
                }
                else if (!string.IsNullOrEmpty(cleanSecAr) && sectorMap.TryGetValue(cleanSecAr, out var sByAr))
                {
                    sector = sByAr;
                }

                // ── Stock resolution ─────────────────────────────────────────
                Stock? existingStock = null;
                bool isNewStock = false;

                if (!string.IsNullOrWhiteSpace(ticker))
                {
                    // Primary lookup: by Ticker (also deactivated rows).
                    existingStock = await _stockRepository.GetByTickerIncludingInactiveAsync(ticker, cancellationToken);
                }

                if (existingStock == null && !string.IsNullOrWhiteSpace(item.NameAr))
                {
                    // Secondary lookup: normalised Arabic name against DB + in-session new stocks.
                    var normKey = ArabicTextNormalizer.Normalize(item.NameAr);
                    if (!string.IsNullOrEmpty(normKey))
                    {
                        // Check in-session first (new stock created earlier in this loop)
                        if (newStocksByNormalizedAr.TryGetValue(normKey, out var inSessionStock))
                        {
                            existingStock = inSessionStock;
                        }
                        else if (byNormalizedAr.TryGetValue(normKey, out var dbStock))
                        {
                            // Found in DB via name — reload with tracking so we can mutate it.
                            existingStock = await _stockRepository.GetByTickerIncludingInactiveAsync(
                                dbStock.Ticker, cancellationToken);
                        }
                    }
                }

                if (existingStock == null)
                {
                    // Create a new stock.
                    // If no ticker was found, generate a stable synthetic one from the Arabic name.
                    if (string.IsNullOrWhiteSpace(ticker))
                    {
                        if (string.IsNullOrWhiteSpace(item.NameAr))
                        {
                            // Truly nothing to identify the stock — skip.
                            skippedStocks++;
                            result.SkippedDetails.Add(new SkippedRowDetail
                            {
                                RowNumber  = item.RowNumber,
                                Identifier = item.NameEn ?? item.NameAr ?? item.SymbolCode,
                                Reason     = "Missing Ticker / SymbolCode and Arabic name"
                            });
                            continue;
                        }

                        // Deterministic synthetic ticker: "AR_" + first 8 chars of normalized name.
                        var norm = ArabicTextNormalizer.Normalize(item.NameAr);
                        ticker = ("AR_" + norm).Substring(0, Math.Min("AR_".Length + norm.Length, 30)).ToUpperInvariant();
                    }

                    existingStock = new Stock
                    {
                        Ticker    = ticker,
                        NameAr    = string.IsNullOrWhiteSpace(item.NameAr) ? ticker : item.NameAr!.Trim(),
                        NameEn    = string.IsNullOrWhiteSpace(item.NameEn) ? null  : item.NameEn!.Trim(),
                        Sector    = sector,
                        SectorId  = sector != null && sector.Id > 0 ? sector.Id : null,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _stockRepository.AddAsync(existingStock, cancellationToken);
                    insertedStocks++;
                    isNewStock = true;

                    // Register in the in-session lookup so duplicate rows in this file don't create a second row.
                    var normNew = ArabicTextNormalizer.Normalize(item.NameAr);
                    if (!string.IsNullOrEmpty(normNew) && !newStocksByNormalizedAr.ContainsKey(normNew))
                        newStocksByNormalizedAr[normNew] = existingStock;
                }
                else if (!isNewStock)
                {
                    bool modified = false;
                    if (!existingStock.IsActive)
                    {
                        existingStock.IsActive         = true;
                        existingStock.DeactivationReason = null;
                        existingStock.DeactivatedAt    = null;
                        modified = true;
                    }
                    if (!string.IsNullOrWhiteSpace(item.NameAr) && existingStock.NameAr != item.NameAr!.Trim())
                    {
                        existingStock.NameAr = item.NameAr!.Trim();
                        modified = true;
                    }
                    if (!string.IsNullOrWhiteSpace(item.NameEn) && existingStock.NameEn != item.NameEn!.Trim())
                    {
                        existingStock.NameEn = item.NameEn!.Trim();
                        modified = true;
                    }
                    if (sector != null && sector.Id > 0 && existingStock.SectorId != sector.Id
                        && !existingStock.IsManual)  // manual sector assignments are never overwritten by uploads
                    {
                        existingStock.SectorId = sector.Id;
                        existingStock.Sector = sector;
                        modified = true;
                    }

                    if (modified)
                    {
                        existingStock.UpdatedAt = DateTime.UtcNow;
                        await _stockRepository.UpdateAsync(existingStock, cancellationToken);
                        updatedStocks++;
                    }
                }

                // Prepare constituent
                var effectiveDate = item.EffectiveDate ?? DateTime.UtcNow.Date;
                constituentsToSave.Add(new IndexConstituent
                {
                    IndexId       = index.Id,
                    Stock         = existingStock,
                    Weight        = isSectoralUpload ? null : item.Weight,
                    EffectiveDate = effectiveDate
                });
            }

            // Save any stock / sector changes first so IDs are resolved
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Re-map StockId if new stock was inserted
            foreach (var constituent in constituentsToSave)
            {
                if (constituent.Stock != null && constituent.StockId == 0)
                {
                    constituent.StockId = constituent.Stock.Id;
                }
            }

            // Replace IndexConstituent rows for this index
            await _indexRepository.ReplaceConstituentsAsync(index.Id, constituentsToSave, cancellationToken);

            // Update Index LastUpdated
            index.LastUpdated = DateTime.UtcNow;
            await _indexRepository.UpdateAsync(index, cancellationToken);

            // Log UploadHistory
            var history = new UploadHistory
            {
                IndexId = index.Id,
                FileName = Path.GetFileName(request.FileName),
                UploadedAt = DateTime.UtcNow,
                RowsAffected = constituentsToSave.Count,
                Status = "Success",
                ErrorMessage = null,
                UploadedBy = request.UploadedBy
            };
            await _uploadHistoryRepository.AddAsync(history, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            result.Inserted = insertedStocks;
            result.Updated = updatedStocks;
            result.Skipped = skippedStocks;
            result.SectorsAdded = sectorsAdded;
            result.TotalConstituents = constituentsToSave.Count;
            result.Status = "Success";
            result.Message = $"Successfully processed {constituentsToSave.Count} constituents for index '{index.Code}'."
                + (sectorsAdded > 0 ? $" {sectorsAdded} new sector(s) created." : string.Empty)
                + (insertedStocks > 0 ? $" {insertedStocks} new stock(s) added." : string.Empty);


            // ── Persist resolved Shariah compliance status for Shariah index ─────
            // When the uploaded file is for the EGX 33 Shariah index, EGX33
            // membership itself is a compliance signal (same weight as a board
            // opinion). Persist this back to ShariahCompliance.Status so every
            // reader that bypasses EffectiveStatus gets the right answer.
            if (IsShariahIndex(index.Code))
            {
                await PersistShariahIndexComplianceAsync(
                    constituentsToSave, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            // Record failed upload history if possible
            try
            {
                var failedHistory = new UploadHistory
                {
                    IndexId = index.Id,
                    FileName = Path.GetFileName(request.FileName),
                    UploadedAt = DateTime.UtcNow,
                    RowsAffected = 0,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    UploadedBy = request.UploadedBy
                };
                await _uploadHistoryRepository.AddAsync(failedHistory, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // Ignore failure to write log
            }

            throw;
        }
    }

    private static string ComputeTicker(string? reutersCode, string symbolCode)
    {
        if (!string.IsNullOrWhiteSpace(reutersCode))
        {
            var trimmed = reutersCode.Trim();
            trimmed = trimmed.TrimEnd('.');
            var dotIndex = trimmed.IndexOf('.');
            if (dotIndex > 0)
            {
                var prefix = trimmed.Substring(0, dotIndex).Trim();
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    return prefix.ToUpperInvariant();
                }
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed.ToUpperInvariant();
            }
        }

        return (symbolCode ?? string.Empty).Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Returns true when <paramref name="indexCode"/> refers to the EGX 33 Shariah index.
    /// The seeded code is "Shariah" (Id = 6); guard against any alternate spellings used
    /// historically in the codebase ("EGX33", "EGX 33").
    /// </summary>
    private static bool IsShariahIndex(string indexCode)
        => string.Equals(indexCode, "Shariah",  StringComparison.OrdinalIgnoreCase)
        || string.Equals(indexCode, "EGX33",    StringComparison.OrdinalIgnoreCase)
        || string.Equals(indexCode, "EGX 33",   StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// For every stock now listed in the Shariah index, persist
    /// <see cref="ShariahCompliance.Status"/> = <see cref="ShariahStatus.Compliant"/>
    /// — unless the activity gate says غير متوافق (CoreActivityCompliant == false),
    /// in which case NonCompliant wins and the stored value is left as-is (or corrected
    /// to NonCompliant if it was stale). EGX33 membership is treated as equal to a
    /// board opinion, so it wins over any previously stored NonCompliant that was not
    /// caused by the activity gate.
    /// </summary>
    private async Task PersistShariahIndexComplianceAsync(
        List<IndexConstituent> constituents,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var constituent in constituents)
        {
            var stockId = constituent.StockId != 0 ? constituent.StockId : constituent.Stock?.Id ?? 0;
            if (stockId == 0) continue;

            var compliance = await _complianceRepository.GetByStockIdAsync(stockId, cancellationToken);
            if (compliance == null)
            {
                // EGX33 membership is itself a compliance signal: a member with no
                // stored row at all (e.g. a newly listed twin tranche) gets one
                // created instead of being left verdict-less. The activity gate
                // still wins over membership (see below).
                var memberMetrics = await _metricsRepository.GetByStockIdAsync(stockId, cancellationToken);
                compliance = new ShariahCompliance
                {
                    StockId = stockId,
                    Status = memberMetrics?.CoreActivityCompliant == false
                        ? ShariahStatus.NonCompliant
                        : ShariahStatus.Compliant,
                    UpdatedAt = now
                };
                await _complianceRepository.AddAsync(compliance, cancellationToken);
                continue;
            }

            var metrics = await _metricsRepository.GetByStockIdAsync(stockId, cancellationToken);

            // Activity gate: CoreActivityCompliant == false always wins.
            if (metrics?.CoreActivityCompliant == false)
            {
                if (compliance.Status != ShariahStatus.NonCompliant)
                {
                    compliance.Status = ShariahStatus.NonCompliant;
                    compliance.UpdatedAt = now;
                    _complianceRepository.Update(compliance);
                }
                continue;
            }

            // EGX33 membership → Compliant (mirrors "any board compliant" rule).
            if (compliance.Status != ShariahStatus.Compliant)
            {
                compliance.Status = ShariahStatus.Compliant;
                compliance.UpdatedAt = now;
                _complianceRepository.Update(compliance);
            }
        }
    }
}
