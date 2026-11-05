const fs = require('fs');
const content = fs.readFileSync('backend/src/Meezan.Application/Features/Shariah/Commands/ImportFaisalOsoulOpinions/ImportFaisalOsoulOpinionsCommand.cs', 'utf8').replace(/\r\n/g, '\n');

const block = `using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

public class FaisalOsoulSourceBlock
{
    [JsonPropertyName("report_date")]
    public string? ReportDate { get; set; }

    [JsonPropertyName("compliant")]
    public List<string> Compliant { get; set; } = new();

    [JsonPropertyName("non_compliant")]
    public List<string> NonCompliant { get; set; } = new();

    [JsonPropertyName("has_shariah_board")]
    public List<string> HasShariahBoard { get; set; } = new();
}
`;

const file = `using System.Text.Json.Serialization;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

/// <summary>Root of shariah_opinions_faisal_osoul.json (the "_notes" block is ignored).</summary>
public class FaisalOsoulOpinionsFile
{
    public FaisalOsoulSourceBlock? FaisalBank { get; set; }
    [JsonPropertyName("ostoul")]
    public FaisalOsoulSourceBlock? Ostoul { get; set; }
}
`;

const srcRes = `using System.Collections.Generic;

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
`;

const mainRes = `using System.Collections.Generic;

namespace Meezan.Application.Features.Shariah.Commands.ImportFaisalOsoulOpinions;

public record ImportFaisalOsoulOpinionsResult(
    bool Success,
    string? Message,
    string FilePath,
    IReadOnlyList<ImportSourceOpinionsResult> Sources);
`;

fs.writeFileSync('backend/src/Meezan.Application/Features/Shariah/Commands/ImportFaisalOsoulOpinions/FaisalOsoulSourceBlock.cs', block, 'utf8');
fs.writeFileSync('backend/src/Meezan.Application/Features/Shariah/Commands/ImportFaisalOsoulOpinions/FaisalOsoulOpinionsFile.cs', file, 'utf8');
fs.writeFileSync('backend/src/Meezan.Application/Features/Shariah/Commands/ImportFaisalOsoulOpinions/ImportSourceOpinionsResult.cs', srcRes, 'utf8');
fs.writeFileSync('backend/src/Meezan.Application/Features/Shariah/Commands/ImportFaisalOsoulOpinions/ImportFaisalOsoulOpinionsResult.cs', mainRes, 'utf8');

const startIdx = content.indexOf('public record ImportSourceOpinionsResult');
const endIdx = content.indexOf('public class ImportFaisalOsoulOpinionsCommandHandler');

const updatedCommand = content.substring(0, startIdx).trimEnd() + '\n\n' + content.substring(endIdx);
fs.writeFileSync('backend/src/Meezan.Application/Features/Shariah/Commands/ImportFaisalOsoulOpinions/ImportFaisalOsoulOpinionsCommand.cs', updatedCommand, 'utf8');
console.log('Done');
