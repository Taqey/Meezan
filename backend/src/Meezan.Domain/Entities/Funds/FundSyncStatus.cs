namespace Meezan.Domain.Entities;

public enum FundSyncStatus
{
    Running = 0,
    Success = 1,
    Partial = 2, // some funds failed
    Failed = 3
}
