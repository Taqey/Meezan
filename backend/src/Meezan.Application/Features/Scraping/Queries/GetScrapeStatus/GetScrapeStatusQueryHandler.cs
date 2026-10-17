using MediatR;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Features.Scraping.DTOs;

namespace Meezan.Application.Features.Scraping.Queries.GetScrapeStatus;

public class GetScrapeStatusQueryHandler : IRequestHandler<GetScrapeStatusQuery, ScrapeStatusResponseDto>
{
    private readonly IScrapeProgressTracker _progress;
    private readonly IScrapeRunLogRepository _runLogRepo;

    public GetScrapeStatusQueryHandler(
        IScrapeProgressTracker progress,
        IScrapeRunLogRepository runLogRepo)
    {
        _progress = progress;
        _runLogRepo = runLogRepo;
    }

    public async Task<ScrapeStatusResponseDto> Handle(GetScrapeStatusQuery request, CancellationToken cancellationToken)
    {
        var all = _progress.GetAllSnapshots();

        // If a specific jobKey was requested, use it; otherwise pick active running job, or default to LiveDaily
        ScrapeProgressSnapshot snapshot;
        if (!string.IsNullOrEmpty(request.JobKey) && all.TryGetValue(request.JobKey, out var specific))
        {
            snapshot = specific;
        }
        else
        {
            var active = all.Values.FirstOrDefault(j => j.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing);
            snapshot = active ?? all.GetValueOrDefault("LiveDaily", _progress.GetSnapshot());
        }

        var jobsDict = all.ToDictionary(
            kvp => kvp.Key,
            kvp => new JobProgressInfoDto(
                JobKey: kvp.Key,
                Status: kvp.Value.Status.ToString(),
                Live: kvp.Value.Status is ScrapeRunStatus.NotRunning ? null : MapLive(kvp.Value),
                FinishedAt: kvp.Value.FinishedAt,
                ErrorReason: kvp.Value.ErrorReason
            )
        );

        // While a run is actively in-flight, return the live state immediately.
        bool isActive = snapshot.Status is ScrapeRunStatus.Running or ScrapeRunStatus.Committing;
        if (isActive)
        {
            return new ScrapeStatusResponseDto(
                Status: snapshot.Status.ToString(),
                Live: MapLive(snapshot),
                LastRun: null,
                Jobs: jobsDict
            );
        }

        // When idle / completed / failed, also pull the last persisted run summary.
        var lastLog = await _runLogRepo.GetLatestAsync(cancellationToken);
        LastRunSummaryDto? lastRun = lastLog is null ? null : new LastRunSummaryDto(
            RunAt: lastLog.StartedAt,
            FinishedAt: lastLog.FinishedAt,
            DurationSeconds: lastLog.FinishedAt.HasValue
                ? (int)(lastLog.FinishedAt.Value - lastLog.StartedAt).TotalSeconds
                : null,
            TotalStocks: lastLog.TotalStocks,
            SucceededStocks: lastLog.SucceededStocks,
            FailedStocks: lastLog.FailedStocks,
            TriggeredBy: lastLog.TriggeredBy,
            HadErrors: lastLog.ErrorSummary is not null
        );

        return new ScrapeStatusResponseDto(
            Status: snapshot.Status.ToString(),
            Live: snapshot.Status is ScrapeRunStatus.NotRunning ? null : MapLive(snapshot),
            LastRun: lastRun,
            Jobs: jobsDict
        );
    }

    private static LiveProgressInfoDto MapLive(ScrapeProgressSnapshot s) => new(
        TriggeredBy: s.TriggeredBy,
        TotalStocks: s.TotalStocks,
        ProcessedCount: s.ProcessedCount,
        SucceededCount: s.SucceededCount,
        FailedCount: s.FailedCount,
        PercentComplete: s.PercentComplete,
        CurrentStockTicker: s.CurrentStockTicker,
        StartedAt: s.StartedAt,
        ElapsedSeconds: s.ElapsedSeconds,
        EstimatedSecondsRemaining: s.EstimatedSecondsRemaining,
        JobKey: s.JobKey
    );
}
