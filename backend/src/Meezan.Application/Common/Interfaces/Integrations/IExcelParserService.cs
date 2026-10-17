using Meezan.Application.Common.Models.Stocks;

namespace Meezan.Application.Common.Interfaces.Integrations;

public interface IExcelParserService
{
    Task<List<ParsedConstituentDto>> ParseIndexConstituentsAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);
}
