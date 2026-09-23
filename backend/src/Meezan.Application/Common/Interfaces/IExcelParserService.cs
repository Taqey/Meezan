using Meezan.Application.Common.Models;

namespace Meezan.Application.Common.Interfaces;

public interface IExcelParserService
{
    Task<List<ParsedConstituentDto>> ParseIndexConstituentsAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
