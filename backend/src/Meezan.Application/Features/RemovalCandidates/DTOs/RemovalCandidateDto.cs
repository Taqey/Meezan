namespace Meezan.Application.Features.RemovalCandidates.DTOs;

public sealed record RemovalCandidateDto(
    int StockId,
    string Ticker,
    string? NameAr,
    string? NameEn,
    DateTime? LastSuccessfulUpdate,
    string? SourceLastUpdateText,
    decimal? LastClosingPrice,
    string Reason,
    bool CurrentIsActive,
    string DataStatus,
    DateTime? DeactivatedAt,
    string? DeactivationReason,
    string? SectorNameAr);
