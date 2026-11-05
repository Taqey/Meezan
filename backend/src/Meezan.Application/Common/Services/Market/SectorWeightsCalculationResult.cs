using System.Collections.Generic;

namespace Meezan.Application.Common.Services.Market;

/// <summary>
/// Result of calculating all sector weights for a given sector.
/// </summary>
public record SectorWeightsCalculationResult(
    int? SectorId,
    List<SectorStockWeightResult> StockWeights,
    decimal TotalWeightSum,
    bool HasSumWarning,
    string? WarningMessage
);
