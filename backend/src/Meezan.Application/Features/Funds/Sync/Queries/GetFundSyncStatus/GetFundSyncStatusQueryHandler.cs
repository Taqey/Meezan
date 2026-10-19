using MediatR;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Results;
using Meezan.Application.Features.Funds.Sync.Dtos;
using Meezan.Domain.Entities;

namespace Meezan.Application.Features.Funds.Sync.Queries.GetFundSyncStatus;

public sealed class GetFundSyncStatusQueryHandler : IRequestHandler<GetFundSyncStatusQuery, Result<FundSyncStatusDto>>
{
    private readonly IFundSyncProgressTracker _tracker;
    private readonly IFundSyncLogRepository _logRepo;

    public GetFundSyncStatusQueryHandler(IFundSyncProgressTracker tracker, IFundSyncLogRepository logRepo)
    {
        _tracker = tracker;
        _logRepo = logRepo;
    }

    public async Task<Result<FundSyncStatusDto>> Handle(GetFundSyncStatusQuery request, CancellationToken cancellationToken)
    {
        var snapshot = _tracker.GetSnapshot();
        if (snapshot.IsRunning)
        {
            return new FundSyncStatusDto(
                IsRunning: true,
                Mode: snapshot.Mode,
                TriggeredBy: snapshot.TriggeredBy,
                StartedAtUtc: snapshot.StartedAtUtc,
                TotalFunds: snapshot.TotalFunds,
                ProcessedCount: snapshot.ProcessedCount,
                UpdatedCount: snapshot.UpdatedCount,
                FailedCount: snapshot.FailedCount,
                CurrentFundCode: snapshot.CurrentFundCode,
                CurrentPhase: snapshot.CurrentPhase,
                FinishedAtUtc: null,
                LastResultSummary: null,
                Error: null);
        }

        // Idle in memory: inspect DB for any running lease or the last finished run
        var latest = await _logRepo.GetLatestAsync(cancellationToken);
        if (latest != null && latest.Status == FundSyncStatus.Running)
        {
            var age = DateTime.UtcNow - latest.RunAt;
            if (age < TimeSpan.FromMinutes(30))
            {
                return new FundSyncStatusDto(
                    IsRunning: true,
                    Mode: latest.Mode,
                    TriggeredBy: latest.TriggeredBy,
                    StartedAtUtc: latest.RunAt,
                    TotalFunds: latest.TotalFunds,
                    ProcessedCount: latest.UpdatedFunds + latest.FailedFunds,
                    UpdatedCount: latest.UpdatedFunds,
                    FailedCount: latest.FailedFunds,
                    CurrentFundCode: null,
                    CurrentPhase: "Running",
                    FinishedAtUtc: null,
                    LastResultSummary: null,
                    Error: null);
            }
        }

        // Finished/idle run
        var lastSummary = snapshot.LastResultSummary;
        if (string.IsNullOrWhiteSpace(lastSummary) && latest != null)
        {
            lastSummary = latest.Status == FundSyncStatus.Success
                ? $"نجح: تم تحديث {latest.UpdatedFunds} من {latest.TotalFunds}"
                : latest.ErrorSummary ?? latest.Status.ToString();
        }

        return new FundSyncStatusDto(
            IsRunning: false,
            Mode: snapshot.Mode ?? latest?.Mode,
            TriggeredBy: snapshot.TriggeredBy ?? latest?.TriggeredBy,
            StartedAtUtc: snapshot.StartedAtUtc ?? latest?.RunAt,
            TotalFunds: snapshot.TotalFunds > 0 ? snapshot.TotalFunds : (latest?.TotalFunds ?? 0),
            ProcessedCount: snapshot.ProcessedCount > 0 ? snapshot.ProcessedCount : ((latest?.UpdatedFunds ?? 0) + (latest?.FailedFunds ?? 0)),
            UpdatedCount: snapshot.UpdatedCount > 0 ? snapshot.UpdatedCount : (latest?.UpdatedFunds ?? 0),
            FailedCount: snapshot.FailedCount > 0 ? snapshot.FailedCount : (latest?.FailedFunds ?? 0),
            CurrentFundCode: null,
            CurrentPhase: "Idle",
            FinishedAtUtc: snapshot.FinishedAtUtc ?? latest?.FinishedAt,
            LastResultSummary: lastSummary,
            Error: snapshot.Error ?? (latest?.Status == FundSyncStatus.Failed ? latest.ErrorSummary : null));
    }
}
