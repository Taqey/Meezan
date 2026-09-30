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

        // 4. Begin transaction for steps: upsert stocks/sectors, replace constituents, update index, log history
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var constituentsToSave = new List<IndexConstituent>();
            int insertedStocks = 0;
            int updatedStocks = 0;
            int skippedStocks = 0;

            foreach (var item in parsedConstituents)
            {
                // Compute ticker: ReutersCode minus exchange suffix (e.g. .CA), else SymbolCode
                var ticker = !string.IsNullOrWhiteSpace(item.Ticker) 
                    ? item.Ticker.Trim().ToUpperInvariant() 
                    : ComputeTicker(item.ReutersCode, item.SymbolCode);

                if (string.IsNullOrWhiteSpace(ticker))
                {
                    skippedStocks++;
                    result.SkippedDetails.Add(new SkippedRowDetail
                    {
                        RowNumber = item.RowNumber,
                        Identifier = item.NameEn ?? item.NameAr ?? item.SymbolCode,
                        Reason = "Missing Ticker / SymbolCode"
                    });
                    continue;
                }

                // Sector handling
                Sector? sector = null;
                if (!string.IsNullOrWhiteSpace(item.SectorEn) || !string.IsNullOrWhiteSpace(item.SectorAr))
                {
                    sector = await _stockRepository.GetOrCreateSectorAsync(item.SectorAr, item.SectorEn, cancellationToken);
                }

                // Stock handling by Ticker
                var existingStock = await _stockRepository.GetByTickerAsync(ticker, cancellationToken);
                if (existingStock == null)
                {
                    existingStock = new Stock
                    {
                        Ticker = ticker,
                        NameAr = string.IsNullOrWhiteSpace(item.NameAr) ? ticker : item.NameAr!.Trim(),
                        NameEn = string.IsNullOrWhiteSpace(item.NameEn) ? ticker : item.NameEn!.Trim(),
                        SectorId = sector?.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _stockRepository.AddAsync(existingStock, cancellationToken);
                    insertedStocks++;
                }
                else
                {
                    bool modified = false;
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
                    if (sector != null && existingStock.SectorId != sector.Id)
                    {
                        existingStock.SectorId = sector.Id;
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
                    IndexId = index.Id,
                    Stock = existingStock,
                    Weight = item.Weight,
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
            result.TotalConstituents = constituentsToSave.Count;
            result.Status = "Success";
            result.Message = $"Successfully processed {constituentsToSave.Count} constituents for index '{index.Code}'.";

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
            if (compliance == null) continue;

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
