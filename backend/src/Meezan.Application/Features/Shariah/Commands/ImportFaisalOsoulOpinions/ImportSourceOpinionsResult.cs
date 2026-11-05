using System.Collections.Generic;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

public record ImportSourceOpinionsResult(
    string SourceKey,
    string? ReportDate,
    int InsertedCount,
    int UpdatedCount,
    int RemovedCount,
    int BoardListedSkippedCount,
    IReadOnlyList<string> UnknownTickers,
    IReadOnlyList<string> Conflicts);
