using System.Collections.Generic;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

public record ImportFaisalOsoulOpinionsResult(
    bool Success,
    string? Message,
    string FilePath,
    IReadOnlyList<ImportSourceOpinionsResult> Sources);
