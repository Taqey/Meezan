using Meezan.Application.Common.Interfaces;
using Meezan.Application.Common.Models;

namespace Meezan.Infrastructure.Tests.Testing;

/// <summary>
/// Scripted ISndukFundClient used by the fund sync characterization tests. Returns
/// canned ("recorded") SNDUK payloads and never touches the network. Individual funds
/// can be told to fail on the per-fund endpoints, mirroring a transport error.
/// </summary>
public sealed class FakeSndukFundClient : ISndukFundClient
{
    public List<SndukFundDto> Funds { get; } = new();

    /// <summary>Price points returned for a fund (any timeframe), keyed by SNDUK id.</summary>
    public Dictionary<long, List<SndukPricePointDto>> Prices { get; } = new();

    /// <summary>Competing (similar) fund ids per SNDUK id.</summary>
    public Dictionary<long, long[]> Competing { get; } = new();

    /// <summary>SNDUK ids whose per-fund performance call throws.</summary>
    public HashSet<long> FailPerformanceFor { get; } = new();

    public int ListCalls { get; private set; }
    public int PerformanceCalls { get; private set; }

    public static SndukFundDto Dto(long id, string code, string price, bool islamic = false, string name = "")
        => new()
        {
            Id = id,
            PublicId = "P" + id,
            Slug = "fund-" + code.ToLowerInvariant(),
            Name = string.IsNullOrEmpty(name) ? "صندوق " + code : name,
            NameEn = "Fund " + code,
            Code = code,
            Type = "equity",
            TypeNameAr = "أسهم",
            TypeNameEn = "Equity",
            RiskLevel = "medium",
            IsIslamic = islamic,
            IsFeatured = false,
            IsActive = true,
            CurrentPrice = price,
            DailyChange = "0.5",
            Returns = new SndukReturnsDto { M1 = "1.0", M3 = "2.0", M6 = "3.0", Y1 = "8.5", Y3 = "20.0" },
            TotalScore = "75.5",
            Aum = "1500000.50",
            ManagementFee = "1.25",
            MinimumInvestment = "1000 EGP",
            Currency = "EGP",
            AssetManagerId = 7,
            AssetManagerName = "مدير",
            AssetManagerNameEn = "Manager",
            AssetManagerCode = "MGR",
            LogoUrl = "https://example.invalid/logo-" + code + ".png",
            Description = "وصف",
            DescriptionEn = "desc",
            InvestmentStrategy = "strategy",
            InceptionDate = "2020-01-01",
            CreatedAt = "2020-01-01",
            UpdatedAt = "2024-01-01"
        };

    public static SndukPricePointDto Point(string date, string price)
        => new() { Date = date, Price = price, Change = "0.1", Performance = "1.0" };

    public Task<SndukFundListResponse?> GetFundListAsync(CancellationToken cancellationToken = default)
    {
        ListCalls++;
        return Task.FromResult<SndukFundListResponse?>(new SndukFundListResponse { Funds = Funds.ToArray() });
    }

    public Task<SndukPerformanceResponse?> GetFundPerformanceAsync(long fundId, string timeframe = "1Y", CancellationToken cancellationToken = default)
    {
        PerformanceCalls++;
        if (FailPerformanceFor.Contains(fundId))
            throw new HttpRequestException("simulated transport failure for fund " + fundId);

        var data = Prices.TryGetValue(fundId, out var points) ? points.ToArray() : [];
        return Task.FromResult<SndukPerformanceResponse?>(new SndukPerformanceResponse { Data = data });
    }

    public Task<SndukAnalysisResponse?> GetFundAnalysisAsync(long fundId, CancellationToken cancellationToken = default)
        => Task.FromResult<SndukAnalysisResponse?>(new SndukAnalysisResponse
        {
            Scores = new SndukScoresDto { Performance = 80, Risk = 60, Cost = 70, Stability = 65, Diversification = -1, Total = 75 },
            RawMetrics = new SndukRawMetricsDto { Cagr = 12.5m, Volatility = 4.2m },
            Explanations = new SndukExplanationsDto
            {
                Performance = new SndukExplanationDto { Ar = ["جيد"], En = ["Good"] }
            },
            CalculatedAt = "2026-10-01T00:00:00Z"
        });

    public Task<SndukCompetingResponse?> GetFundCompetingAsync(long fundId, CancellationToken cancellationToken = default)
    {
        var ids = Competing.TryGetValue(fundId, out var found) ? found : [];
        return Task.FromResult<SndukCompetingResponse?>(new SndukCompetingResponse
        {
            Funds = ids.Select(i => new SndukFundDto { Id = i }).ToArray()
        });
    }
}
