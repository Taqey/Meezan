namespace Meezan.Application.Features.Funds.Sync.Dtos;

public sealed record FundCompareChartDto(
    int FundId,
    string FundCode,
    string FundNameAr,
    string Currency,
    List<FundCompareSeriesPointDto> Points);
