using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Meezan.Application.Common.Interfaces;
using Meezan.Application.Common.Models;

namespace Meezan.Infrastructure.Services;

public class ExcelParserService : IExcelParserService
{
    public async Task<List<ParsedConstituentDto>> ParseIndexConstituentsAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        if (IsHtmlFormat(ms))
        {
            ms.Position = 0;
            return ParseHtmlTable(ms);
        }

        ms.Position = 0;
        return ParseOpenXmlExcel(ms);
    }

    private static bool IsHtmlFormat(Stream stream)
    {
        var buffer = new byte[512];
        int read = stream.Read(buffer, 0, buffer.Length);
        stream.Position = 0;
        if (read <= 0) return false;

        var prefix = Encoding.UTF8.GetString(buffer, 0, read).TrimStart();
        return prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
               prefix.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
               prefix.StartsWith("<table", StringComparison.OrdinalIgnoreCase);
    }

    private static List<ParsedConstituentDto> ParseOpenXmlExcel(Stream stream)
    {
        var results = new List<ParsedConstituentDto>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return results;
        }

        IXLRow? headerRow = null;
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int r = 1; r <= 15; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty()) continue;

            var tempMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in row.CellsUsed())
            {
                var text = cell.GetString().Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    tempMap[text] = cell.Address.ColumnNumber;
                }
            }

            if (HasConstituentHeaders(tempMap))
            {
                headerRow = row;
                headerMap = tempMap;
                break;
            }
        }

        if (headerRow == null)
        {
            return results;
        }

        int colSymbol = FindColumn(headerMap, "SYMBOL_CODE", "SYMBOL", "ISIN", "كود الترقيم الدولى", "كود الترقيم الدولي", "كود السهم");
        int colNameAr = FindColumn(headerMap, "SYMB_NAME_ARB", "NAME_ARB", "اسم الشركة", "الشركة", "اسم السهم");
        int colNameEn = FindColumn(headerMap, "SYMB_NAME_ENG", "NAME_ENG", "Company Name", "Security Name");
        int colReuters = FindColumn(headerMap, "REUTERS_CODE", "REUTERS", "كود رويترز", "Ticker");
        int colSectorAr = FindColumn(headerMap, "SECTOR_ARB", "القطاع");
        int colSectorEn = FindColumn(headerMap, "SECTOR_ENG", "Sector");
        int colWeight = FindWeightColumn(headerMap, out DateTime? extractedEffectiveDate);

        int startRow = headerRow.RowNumber() + 1;
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? startRow;

        for (int r = startRow; r <= lastRow; r++)
        {
            var row = worksheet.Row(r);
            if (row.IsEmpty()) continue;

            var symbolCode = colSymbol > 0 ? row.Cell(colSymbol).GetString().Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(symbolCode)) continue;

            var nameAr = colNameAr > 0 ? row.Cell(colNameAr).GetString().Trim() : string.Empty;
            var nameEn = colNameEn > 0 ? row.Cell(colNameEn).GetString().Trim() : string.Empty;
            var reuters = colReuters > 0 ? row.Cell(colReuters).GetString().Trim() : null;
            var sectorAr = colSectorAr > 0 ? row.Cell(colSectorAr).GetString().Trim() : null;
            var sectorEn = colSectorEn > 0 ? row.Cell(colSectorEn).GetString().Trim() : null;

            decimal? weight = null;
            if (colWeight > 0)
            {
                var cell = row.Cell(colWeight);
                weight = ParseDecimal(cell.GetString());
            }

            var ticker = ComputeTicker(reuters, symbolCode);

            results.Add(new ParsedConstituentDto
            {
                Ticker = ticker,
                SymbolCode = symbolCode,
                NameAr = string.IsNullOrWhiteSpace(nameAr) ? (string.IsNullOrWhiteSpace(nameEn) ? ticker : nameEn) : nameAr,
                NameEn = string.IsNullOrWhiteSpace(nameEn) ? (string.IsNullOrWhiteSpace(nameAr) ? ticker : nameAr) : nameEn,
                ReutersCode = reuters,
                SectorAr = sectorAr,
                SectorEn = sectorEn,
                Weight = weight,
                EffectiveDate = extractedEffectiveDate,
                RowNumber = r
            });
        }

        return results;
    }

    private static List<ParsedConstituentDto> ParseHtmlTable(Stream stream)
    {
        var results = new List<ParsedConstituentDto>();
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
        var html = reader.ReadToEnd();

        var rowMatches = Regex.Matches(html, @"<tr[^>]*>(.*?)</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (rowMatches.Count == 0) return results;

        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int headerRowIndex = -1;

        for (int i = 0; i < Math.Min(rowMatches.Count, 10); i++)
        {
            var trContent = rowMatches[i].Groups[1].Value;
            var cellMatches = Regex.Matches(trContent, @"<(th|td)[^>]*>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (cellMatches.Count == 0) continue;

            var tempMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int col = 0; col < cellMatches.Count; col++)
            {
                var cellText = StripHtml(cellMatches[col].Groups[2].Value);
                if (!string.IsNullOrEmpty(cellText))
                {
                    tempMap[cellText] = col;
                }
            }

            if (HasConstituentHeaders(tempMap))
            {
                headerMap = tempMap;
                headerRowIndex = i;
                break;
            }
        }

        if (headerRowIndex == -1) return results;

        int colSymbol = FindColumn(headerMap, "كود الترقيم الدولى", "كود الترقيم الدولي", "SYMBOL_CODE", "SYMBOL", "ISIN");
        int colReuters = FindColumn(headerMap, "كود رويترز", "REUTERS_CODE", "REUTERS");
        int colNameAr = FindColumn(headerMap, "اسم الشركة", "SYMB_NAME_ARB", "NAME_ARB");
        int colNameEn = FindColumn(headerMap, "SYMB_NAME_ENG", "NAME_ENG");
        int colSectorAr = FindColumn(headerMap, "القطاع", "SECTOR_ARB");
        int colSectorEn = FindColumn(headerMap, "SECTOR_ENG", "Sector");
        int colWeight = FindWeightColumn(headerMap, out DateTime? extractedEffectiveDate);

        for (int i = headerRowIndex + 1; i < rowMatches.Count; i++)
        {
            var trContent = rowMatches[i].Groups[1].Value;
            var cellMatches = Regex.Matches(trContent, @"<(th|td)[^>]*>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (cellMatches.Count == 0) continue;

            string GetCell(int colIdx) =>
                colIdx >= 0 && colIdx < cellMatches.Count ? StripHtml(cellMatches[colIdx].Groups[2].Value) : string.Empty;

            var symbolCode = GetCell(colSymbol);
            if (string.IsNullOrWhiteSpace(symbolCode)) continue;

            var reuters = GetCell(colReuters);
            var nameAr = GetCell(colNameAr);
            var nameEn = GetCell(colNameEn);
            var sectorAr = GetCell(colSectorAr);
            var sectorEn = GetCell(colSectorEn);
            var weightStr = GetCell(colWeight);

            // Null (not 0) when the file has no weight column at all.
            decimal? weight = colWeight >= 0 ? ParseDecimal(weightStr) : null;
            var ticker = ComputeTicker(reuters, symbolCode);

            results.Add(new ParsedConstituentDto
            {
                Ticker = ticker,
                SymbolCode = symbolCode,
                NameAr = string.IsNullOrWhiteSpace(nameAr) ? (string.IsNullOrWhiteSpace(nameEn) ? ticker : nameEn) : nameAr,
                NameEn = string.IsNullOrWhiteSpace(nameEn) ? (string.IsNullOrWhiteSpace(nameAr) ? ticker : nameAr) : nameEn,
                ReutersCode = string.IsNullOrWhiteSpace(reuters) ? null : reuters,
                SectorAr = string.IsNullOrWhiteSpace(sectorAr) ? null : sectorAr,
                SectorEn = string.IsNullOrWhiteSpace(sectorEn) ? null : sectorEn,
                Weight = weight,
                EffectiveDate = extractedEffectiveDate,
                RowNumber = i + 1
            });
        }

        return results;
    }

    public static string ComputeTicker(string? reutersCode, string symbolCode)
    {
        if (!string.IsNullOrWhiteSpace(reutersCode))
        {
            var trimmed = reutersCode.Trim();
            // Remove any trailing dots or spaces
            trimmed = trimmed.TrimEnd('.');
            // If it contains a dot (e.g. AMOC.CA or NDRL.CA), take the prefix
            var dotIndex = trimmed.IndexOf('.');
            if (dotIndex > 0)
            {
                var prefix = trimmed.Substring(0, dotIndex).Trim();
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    return prefix.ToUpperInvariant();
                }
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                return trimmed.ToUpperInvariant();
            }
        }

        return (symbolCode ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool HasConstituentHeaders(Dictionary<string, int> map)
    {
        return map.Keys.Any(k =>
            k.Contains("SYMBOL", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("الترقيم الدول", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("اسم الشركة", StringComparison.OrdinalIgnoreCase) ||
            k.Contains("REUTERS", StringComparison.OrdinalIgnoreCase));
    }

    private static int FindColumn(Dictionary<string, int> map, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            foreach (var kvp in map)
            {
                if (string.Equals(kvp.Key, candidate, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
        }
        return -1;
    }

    private static int FindWeightColumn(Dictionary<string, int> map, out DateTime? effectiveDate)
    {
        effectiveDate = null;
        foreach (var kvp in map)
        {
            var key = kvp.Key;
            if (key.Contains("weight", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("وزن", StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(key, @"\d{1,2}[/-]\d{1,2}[/-]\d{2,4}");
                if (match.Success && DateTime.TryParse(match.Value, new CultureInfo("en-GB"), out var parsedDate))
                {
                    effectiveDate = parsedDate;
                }
                return kvp.Value;
            }
        }
        return -1;
    }

    private static decimal ParseDecimal(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;

        var clean = text.Replace("%", "").Trim();
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            return val;
        }

        if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var dblVal))
        {
            return (decimal)dblVal;
        }

        return 0m;
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var stripped = Regex.Replace(input, @"<[^>]*>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(stripped).Trim();
    }
}
