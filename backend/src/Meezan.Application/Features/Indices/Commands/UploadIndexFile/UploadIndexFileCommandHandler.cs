using Meezan.Application.Common.Interfaces;
using Meezan.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Meezan.Application.Features.Indices.Commands.UploadIndexFile;

public class UploadIndexFileCommandHandler : IRequestHandler<UploadIndexFileCommand, UploadIndexFileResultDto>
{
    private readonly IIndexRepository _indexRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IUploadHistoryRepository _uploadHistoryRepository;
    private readonly IExcelParserService _excelParserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UploadIndexFileCommand> _validator;

    public UploadIndexFileCommandHandler(
        IIndexRepository indexRepository,
        IStockRepository stockRepository,
        IUploadHistoryRepository uploadHistoryRepository,
        IExcelParserService excelParserService,
        IUnitOfWork unitOfWork,
        IValidator<UploadIndexFileCommand> validator)
    {
        _indexRepository = indexRepository;
        _stockRepository = stockRepository;
        _uploadHistoryRepository = uploadHistoryRepository;
        _excelParserService = excelParserService;
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
}
