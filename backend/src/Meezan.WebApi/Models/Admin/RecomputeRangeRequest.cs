namespace Meezan.WebApi.Models.Admin;

public sealed record RecomputeRangeRequest(DateOnly FromDate, DateOnly ToDate);
