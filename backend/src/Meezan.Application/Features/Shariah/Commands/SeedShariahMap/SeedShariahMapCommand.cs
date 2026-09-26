using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Shariah.DTOs;
using Meezan.Domain.Entities;
using Meezan.Domain.Enums;

namespace Meezan.Application.Features.Shariah.Commands.SeedShariahMap;

public record SeedShariahMapCommand(Dictionary<string, ShariahSeedItemDto>? Data = null) : IRequest<SeedShariahMapResult>;

public record SeedShariahMapResult(int InsertedCount, int UpdatedCount, int TotalProcessed);

public class SeedShariahMapCommandHandler : IRequestHandler<SeedShariahMapCommand, SeedShariahMapResult>
{
    private readonly IStockRepository _stockRepository;
    private readonly IShariahComplianceRepository _complianceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SeedShariahMapCommandHandler(
        IStockRepository stockRepository,
        IShariahComplianceRepository complianceRepository,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _complianceRepository = complianceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SeedShariahMapResult> Handle(SeedShariahMapCommand request, CancellationToken cancellationToken)
    {
        if (request.Data == null || request.Data.Count == 0)
        {
            return new SeedShariahMapResult(0, 0, 0);
        }

        int inserted = 0;
        int updated = 0;
        var now = DateTime.UtcNow;

        foreach (var (rawSymbol, item) in request.Data)
        {
            var symbol = rawSymbol.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(symbol)) continue;

            // 1. Check or create Stock
            var stock = await _stockRepository.GetBySymbolCodeAsync(symbol, cancellationToken);
            if (stock == null)
            {
                stock = new Stock
                {
                    Ticker = symbol,
                    // Leave NameAr/NameEn null — real names will be populated by index upload.
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _stockRepository.AddAsync(stock, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 2. Parse status
            var status = ParseStatus(item.Status);

            // Board governance indicated by the seed note (plain "لجنة شرعية" and accredited
            // "هيئة رقابة شرعية داخلية معتمدة" are the same case).
            bool hasBoard = Common.ShariahBoardDetector.IsBoardNote(item.Note);

            // 3. Upsert ShariahCompliance
            var compliance = await _complianceRepository.GetByStockIdAsync(stock.Id, cancellationToken);
            if (compliance == null)
            {
                compliance = new ShariahCompliance
                {
                    StockId = stock.Id,
                    Status = status,
                    Pct = item.Pct,
                    Note = item.Note,
                    HasShariahBoard = hasBoard,
                    LastCheckedAt = now,
                    UpdatedAt = now
                };
                await _complianceRepository.AddAsync(compliance, cancellationToken);
                inserted++;
            }
            else
            {
                compliance.Status = status;
                compliance.Pct = item.Pct;
                compliance.Note = item.Note;
                compliance.HasShariahBoard = hasBoard;
                compliance.LastCheckedAt = now;
                compliance.UpdatedAt = now;
                _complianceRepository.Update(compliance);
                updated++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new SeedShariahMapResult(inserted, updated, request.Data.Count);
    }

    private static ShariahStatus ParseStatus(string? statusStr)
    {
        if (string.IsNullOrWhiteSpace(statusStr)) return ShariahStatus.Pending;

        var normalized = statusStr.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
        return normalized switch
        {
            "compliant" => ShariahStatus.Compliant,
            "non_compliant" => ShariahStatus.NonCompliant,
            "noncompliant" => ShariahStatus.NonCompliant,
            "blocked" => ShariahStatus.Blocked,
            _ => ShariahStatus.Pending
        };
    }
}
