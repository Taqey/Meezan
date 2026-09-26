using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Common.Models;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Shariah.Commands.RefreshShariahData;

public record RefreshShariahDataCommand(string TriggeredBy = "Manual") : IRequest<RefreshShariahDataResult>;

public record RefreshShariahDataResult(
    bool Success,
    string? Message,
    int PctUpdatedCount,
    int SkippedNoValueCount,
    int SkippedNotFoundCount,
    int StocksFullyRefreshedCount);

public class RefreshShariahDataCommandHandler : IRequestHandler<RefreshShariahDataCommand, RefreshShariahDataResult>
{
    private readonly IShariahSourceClient _sourceClient;
    private readonly IStockRepository _stockRepository;
    private readonly IShariahComplianceRepository _complianceRepository;
    private readonly IShariahSourceOpinionRepository _opinionRepository;
    private readonly IStockShariahMetricsRepository _metricsRepository;
    private readonly IShariahRefreshLogRepository _logRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshShariahDataCommandHandler(
        IShariahSourceClient sourceClient,
        IStockRepository stockRepository,
        IShariahComplianceRepository complianceRepository,
        IShariahSourceOpinionRepository opinionRepository,
        IStockShariahMetricsRepository metricsRepository,
        IShariahRefreshLogRepository logRepository,
        IUnitOfWork unitOfWork)
    {
        _sourceClient = sourceClient;
        _stockRepository = stockRepository;
        _complianceRepository = complianceRepository;
        _opinionRepository = opinionRepository;
        _metricsRepository = metricsRepository;
        _logRepository = logRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RefreshShariahDataResult> Handle(RefreshShariahDataCommand request, CancellationToken cancellationToken)
    {
        List<ExternalStockMergedDto> externalData;
        try
        {
            externalData = await _sourceClient.FetchMergedStocksAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Log run failure gracefully
            var failLog = new ShariahRefreshLog
            {
                RunAt = DateTime.UtcNow,
                TriggeredBy = request.TriggeredBy,
                PctUpdatedCount = 0,
                SkippedNoValueCount = 0,
                SkippedNotFoundCount = 0,
                StocksFullyRefreshedCount = 0
            };
            await _logRepository.AddAsync(failLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RefreshShariahDataResult(false, $"Failed to fetch external shariah data: {ex.Message}", 0, 0, 0, 0);
        }

        int pctUpdatedCount = 0;
        int skippedNoValueCount = 0;
        int skippedNotFoundCount = 0;
        int stocksFullyRefreshedCount = 0;
        var now = DateTime.UtcNow;

        foreach (var item in externalData)
        {
            if (string.IsNullOrWhiteSpace(item.Symbol)) continue;
            var symbol = item.Symbol.Trim().ToUpperInvariant();

            // 1. Find or create Stock
            var stock = await _stockRepository.GetBySymbolCodeAsync(symbol, cancellationToken);
            if (stock == null)
            {
                stock = new Stock
                {
                    Ticker = symbol,
                    // Only set names when the external source has real data.
                    // Leave null if the external name is empty — real names come from index upload.
                    NameAr = !string.IsNullOrWhiteSpace(item.NameAr) ? item.NameAr : null,
                    NameEn = !string.IsNullOrWhiteSpace(item.NameEn) ? item.NameEn : null,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _stockRepository.AddAsync(stock, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // Backfill names when missing or still holding the old symbol-code placeholder.
                bool modified = false;
                bool nameArMissing = string.IsNullOrWhiteSpace(stock.NameAr) || stock.NameAr == stock.Ticker;
                bool nameEnMissing = string.IsNullOrWhiteSpace(stock.NameEn) || stock.NameEn == stock.Ticker;

                if (nameArMissing && !string.IsNullOrWhiteSpace(item.NameAr))
                {
                    stock.NameAr = item.NameAr;
                    modified = true;
                }
                if (nameEnMissing && !string.IsNullOrWhiteSpace(item.NameEn))
                {
                    stock.NameEn = item.NameEn;
                    modified = true;
                }
                if (modified)
                {
                    await _stockRepository.UpdateAsync(stock, cancellationToken);
                }
            }

            // 2. Upsert StockShariahMetrics (always overwrite)
            var metrics = await _metricsRepository.GetByStockIdAsync(stock.Id, cancellationToken);
            if (metrics == null)
            {
                metrics = new StockShariahMetrics
                {
                    StockId = stock.Id,
                    Zakat = item.Zakat,
                    SpHaramEarningPercentage = item.SpHaramEarningPercentage,
                    AaoifiHaramEarningPerShare = item.AaoifiHaramEarningPerShare,
                    HaramEarningsPercentage = item.HaramEarningsPercentage,
                    LoansPercentage = item.LoansPercentage,
                    FairValueValuation = item.FairValueValuation,
                    BookValue = item.BookValue,
                    Profit = item.Profit,
                    Dividend = item.Dividend,
                    DividendType = item.DividendType,
                    CoreActivityCompliant = item.CoreActivityCompliant,
                    CashLiquidityCompliant = item.CashLiquidityCompliant,
                    HaramInvestmentsCompliant = item.HaramInvestmentsCompliant,
                    CategoryEn = item.Category,
                    CategoryAr = item.CategoryAr,
                    SourceUpdatedAt = item.UpdatedAt ?? item.LastUpdated,
                    FetchedAt = now
                };
                await _metricsRepository.AddAsync(metrics, cancellationToken);
            }
            else
            {
                metrics.Zakat = item.Zakat;
                metrics.SpHaramEarningPercentage = item.SpHaramEarningPercentage;
                metrics.AaoifiHaramEarningPerShare = item.AaoifiHaramEarningPerShare;
                metrics.HaramEarningsPercentage = item.HaramEarningsPercentage;
                metrics.LoansPercentage = item.LoansPercentage;
                metrics.FairValueValuation = item.FairValueValuation;
                metrics.BookValue = item.BookValue;
                metrics.Profit = item.Profit;
                metrics.Dividend = item.Dividend;
                metrics.DividendType = item.DividendType;
                metrics.CoreActivityCompliant = item.CoreActivityCompliant;
                metrics.CashLiquidityCompliant = item.CashLiquidityCompliant;
                metrics.HaramInvestmentsCompliant = item.HaramInvestmentsCompliant;
                metrics.CategoryEn = item.Category;
                metrics.CategoryAr = item.CategoryAr;
                metrics.SourceUpdatedAt = item.UpdatedAt ?? item.LastUpdated;
                metrics.FetchedAt = now;
                _metricsRepository.Update(metrics);
            }

            // 3. Upsert ShariahSourceOpinions (overwrite/replace for each source key)
            var existingOpinions = await _opinionRepository.GetByStockIdAsync(stock.Id, cancellationToken);
            var opinionsDict = existingOpinions.ToDictionary(o => o.SourceKey);

            UpdateOrAddOpinion(opinionsDict, stock.Id, ShariahSourceKey.HalalBourse, item.HalalBourse, now);
            UpdateOrAddOpinion(opinionsDict, stock.Id, ShariahSourceKey.Musaffa, item.Musaffa, now);
            UpdateOrAddOpinion(opinionsDict, stock.Id, ShariahSourceKey.Kashif, item.Kashif, now);
            UpdateOrAddOpinion(opinionsDict, stock.Id, ShariahSourceKey.HalalInvest, item.HalalInvest, now);
            UpdateOrAddOpinion(opinionsDict, stock.Id, ShariahSourceKey.FaisalBank, item.FaisalBank, now);
            UpdateOrAddOpinion(opinionsDict, stock.Id, ShariahSourceKey.Osoul, item.Osoul, now);
            UpdateOrAddOpinion(opinionsDict, stock.Id, ShariahSourceKey.Thndr, item.Thndr, now);

            // 4. Update ShariahCompliance:
            // "only backfill Pct from sp_haram_earning_percentage when the stock's Status is Compliant AND Pct is currently null.
            // Never overwrite an already-set Pct. Everything else on ShariahCompliance is untouched by this job."
            // Exception: HasShariahBoard is recomputed on every run so a stock that gains (or
            // loses) a board/committee verdict in the source is displayed accordingly.
            var compliance = await _complianceRepository.GetByStockIdAsync(stock.Id, cancellationToken);
            if (compliance == null)
            {
                skippedNotFoundCount++;
            }
            else
            {
                bool dirty = false;

                bool hasBoardNow = Common.ShariahBoardDetector.IsBoardNote(compliance.Note)
                    || opinionsDict.Values.Any(o => Common.ShariahBoardDetector.IsBoardNote(o.Note));
                if (compliance.HasShariahBoard != hasBoardNow)
                {
                    compliance.HasShariahBoard = hasBoardNow;
                    dirty = true;
                }

                if (compliance.Status == ShariahStatus.Compliant && compliance.Pct == null)
                {
                    if (item.SpHaramEarningPercentage.HasValue)
                    {
                        compliance.Pct = item.SpHaramEarningPercentage.Value;
                        dirty = true;
                        pctUpdatedCount++;
                    }
                    else
                    {
                        skippedNoValueCount++;
                    }
                }

                if (dirty)
                {
                    compliance.UpdatedAt = now;
                    _complianceRepository.Update(compliance);
                }
            }

            stocksFullyRefreshedCount++;
        }

        var log = new ShariahRefreshLog
        {
            RunAt = now,
            TriggeredBy = request.TriggeredBy,
            PctUpdatedCount = pctUpdatedCount,
            SkippedNoValueCount = skippedNoValueCount,
            SkippedNotFoundCount = skippedNotFoundCount,
            StocksFullyRefreshedCount = stocksFullyRefreshedCount
        };
        await _logRepository.AddAsync(log, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshShariahDataResult(
            true,
            "Shariah data refreshed successfully",
            pctUpdatedCount,
            skippedNoValueCount,
            skippedNotFoundCount,
            stocksFullyRefreshedCount);
    }

    private void UpdateOrAddOpinion(
        Dictionary<ShariahSourceKey, ShariahSourceOpinion> existingDict,
        int stockId,
        ShariahSourceKey key,
        ExternalSourceOpinionDto? dto,
        DateTime fetchedAt)
    {
        if (dto == null) return;

        if (existingDict.TryGetValue(key, out var existing))
        {
            existing.Status = dto.Status;
            existing.Percentage = dto.Percentage;
            existing.Note = dto.Note;
            existing.PdfUrl = Common.ShariahPdfUrlNormalizer.Normalize(dto.PdfUrl);
            existing.SourceLastUpdated = dto.LastUpdated;
            existing.FetchedAt = fetchedAt;
            if (!string.IsNullOrEmpty(dto.ExtraDataJson))
            {
                existing.ExtraData = dto.ExtraDataJson;
            }
            _opinionRepository.Update(existing);
        }
        else
        {
            var opinion = new ShariahSourceOpinion
            {
                StockId = stockId,
                SourceKey = key,
                Status = dto.Status,
                Percentage = dto.Percentage,
                Note = dto.Note,
                PdfUrl = Common.ShariahPdfUrlNormalizer.Normalize(dto.PdfUrl),
                SourceLastUpdated = dto.LastUpdated,
                FetchedAt = fetchedAt,
                ExtraData = dto.ExtraDataJson
            };
            _ = _opinionRepository.AddAsync(opinion);
        }
    }
}
