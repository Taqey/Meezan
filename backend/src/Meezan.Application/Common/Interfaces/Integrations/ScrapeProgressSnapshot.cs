using System;

namespace Meezan.Application.Common.Interfaces.Integrations;

/// <summary>
/// Immutable snapshot returned to callers — no lock needed once constructed.
/// Derived fields are pre-computed server-side so the frontend can bind directly.
/// </summary>
public sealed record ScrapeProgressSnapshot(
    ScrapeRunStatus Status,
    string? TriggeredBy,
    int TotalStocks,
    int ProcessedCount,
    int SucceededCount,
    int FailedCount,
    double PercentComplete,
    string? CurrentStockTicker,
    DateTime? StartedAt,
    double ElapsedSeconds,
    double? EstimatedSecondsRemaining,
    string JobKey = "LiveDaily",
    DateTime? FinishedAt = null,
    string? ErrorReason = null
);
