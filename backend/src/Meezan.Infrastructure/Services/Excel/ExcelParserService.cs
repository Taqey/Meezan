using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Meezan.Application.Common.Interfaces.Integrations;
using Meezan.Application.Common.Interfaces.Persistence;
using Meezan.Application.Common.Interfaces.Queries.Funds;
using Meezan.Application.Common.Interfaces.Queries.RemovalCandidates;
using Meezan.Application.Common.Interfaces.Queries.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.Funds;
using Meezan.Application.Common.Interfaces.Repositories.Indices;
using Meezan.Application.Common.Interfaces.Repositories.Market;
using Meezan.Application.Common.Interfaces.Repositories.Scraping;
using Meezan.Application.Common.Interfaces.Repositories.Sectors;
using Meezan.Application.Common.Interfaces.Repositories.Shariah;
using Meezan.Application.Common.Interfaces.Repositories.Stocks;
using Meezan.Application.Common.Interfaces.Repositories.System;
using Meezan.Application.Common.Interfaces.Security;
using Meezan.Application.Common.Interfaces.Storage;
using Meezan.Application.Common.Models.Funds;
using Meezan.Application.Common.Models.Stocks;

namespace Meezan.Infrastructure.Services.Excel;

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
            var nameAr = colNameAr > 0 ? row.Cell(colNameAr).GetString().Trim() : string.Empty;
            var nameEn = colNameEn > 0 ? row.Cell(colNameEn).GetString().Trim() : string.Empty;
            var reuters = colReuters > 0 ? (row.Cell(colReuters).GetString().Trim()) : null;
            if (string.IsNullOrWhiteSpace(reuters)) reuters = null;
            var sectorAr = colSectorAr > 0 ? (row.Cell(colSectorAr).GetString().Trim()) : null;
            if (string.IsNullOrWhiteSpace(sectorAr)) sectorAr = null;
            var sectorEn = colSectorEn > 0 ? (row.Cell(colSectorEn).GetString().Trim()) : null;
            if (string.IsNullOrWhiteSpace(sectorEn)) sectorEn = null;

            decimal? weight = null;
            if (colWeight > 0)
            {
                var cell = row.Cell(colWeight);
                weight = ParseCellWeight(cell);
            }

            // Skip blank or TOTAL rows:
            // TOTAL row has only weight filled (symbolCode, nameAr, nameEn, reuters, sectorAr, sectorEn all empty)
            // or name explicitly says "TOTAL" / "المجموع" / "الإجمالي"
            if (string.IsNullOrWhiteSpace(symbolCode) &&
                string.IsNullOrWhiteSpace(nameAr) &&
                string.IsNullOrWhiteSpace(nameEn) &&
                string.IsNullOrWhiteSpace(reuters) &&
                string.IsNullOrWhiteSpace(sectorAr) &&
                string.IsNullOrWhiteSpace(sectorEn))
            {
                continue; // Blank or TOTAL row
            }

            if (nameAr.Equals("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                nameEn.Equals("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                nameAr.Equals("المجموع", StringComparison.OrdinalIgnoreCase) ||
                nameAr.Equals("الإجمالي", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Row must have at least an ISIN/SymbolCode OR an Arabic name
            if (string.IsNullOrWhiteSpace(symbolCode) && string.IsNullOrWhiteSpace(nameAr))
            {
                continue;
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

        // Apply column-level convention normalization to ensure weights are consistently in percentage points
        NormalizeConstituentWeights(results);

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

            var symbolCode = GetCell(colSymbol).Trim();
            var reuters = GetCell(colReuters).Trim();
            if (string.IsNullOrWhiteSpace(reuters)) reuters = null;
            var nameAr = GetCell(colNameAr).Trim();
            var nameEn = GetCell(colNameEn).Trim();
            var sectorAr = GetCell(colSectorAr).Trim();
            if (string.IsNullOrWhiteSpace(sectorAr)) sectorAr = null;
            var sectorEn = GetCell(colSectorEn).Trim();
            if (string.IsNullOrWhiteSpace(sectorEn)) sectorEn = null;
            var weightStr = GetCell(colWeight).Trim();

            // Skip blank or TOTAL rows
            if (string.IsNullOrWhiteSpace(symbolCode) &&
                string.IsNullOrWhiteSpace(nameAr) &&
                string.IsNullOrWhiteSpace(nameEn) &&
                string.IsNullOrWhiteSpace(reuters) &&
                string.IsNullOrWhiteSpace(sectorAr) &&
                string.IsNullOrWhiteSpace(sectorEn))
            {
                continue;
            }

            if (nameAr.Equals("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                nameEn.Equals("TOTAL", StringComparison.OrdinalIgnoreCase) ||
                nameAr.Equals("المجموع", StringComparison.OrdinalIgnoreCase) ||
                nameAr.Equals("الإجمالي", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(symbolCode) && string.IsNullOrWhiteSpace(nameAr))
            {
                continue;
            }

            // Null (not 0) when the file has no weight column at all.
            decimal? weight = colWeight >= 0 && !string.IsNullOrWhiteSpace(weightStr) ? ParseDecimal(weightStr) : null;
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
                RowNumber = i + 1
            });
        }

        // Apply column-level convention normalization
        NormalizeConstituentWeights(results);

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

    private static decimal? ParseCellWeight(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        bool isPercentFormatted = false;
        try
        {
            var numFormat = cell.Style.NumberFormat.Format;
            if (!string.IsNullOrEmpty(numFormat) && numFormat.Contains('%'))
            {
                isPercentFormatted = true;
            }
        }
        catch
        {
            // Ignore format inspection failure
        }

        // Try getting underlying numeric value first
        if (cell.TryGetValue<decimal>(out var numVal))
        {
            if (isPercentFormatted)
            {
                // In Excel, 35% is stored as 0.35 with percent format. Convert to 35.
                return numVal * 100m;
            }
            return numVal;
        }

        if (cell.TryGetValue<double>(out var dblVal))
        {
            var dec = (decimal)dblVal;
            if (isPercentFormatted)
            {
                return dec * 100m;
            }
            return dec;
        }

        // Fall back to string parsing
        var text = cell.GetString().Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        return ParseTextWeight(text);
    }

    public static decimal? ParseTextWeight(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var raw = text.Trim();
        bool hasPercentSign = raw.Contains('%');

        // Remove percent sign and extra whitespace
        var cleaned = raw.Replace("%", string.Empty).Trim();

        // Support decimal comma (e.g. "35,5" -> "35.5") if no dot is present, or if comma is decimal separator
        if (cleaned.Contains(',') && !cleaned.Contains('.'))
        {
            cleaned = cleaned.Replace(',', '.');
        }

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            // If text had a "%" symbol (e.g. "35%", "35 %"), val is 35 (already in percentage points)
            return val;
        }

        if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
        {
            return (decimal)dbl;
        }

        return null;
    }

    /// <summary>
    /// Detects whether weights were supplied as fractions (summing to ~1) vs percentage points (summing to ~100)
    /// and normalizes them column-wide so all weights are represented as percentage points.
    /// Handles files with multiple sector columns/groups as well as single-index files.
    /// </summary>
    public static void NormalizeConstituentWeights(List<ParsedConstituentDto> constituents)
    {
        if (constituents == null || constituents.Count == 0) return;

        var itemsWithWeight = constituents.Where(c => c.Weight.HasValue && c.Weight.Value > 0).ToList();
        if (itemsWithWeight.Count == 0) return;

        // Check if constituents belong to distinct sectors (e.g. Sectoral-Indices file)
        var sectors = constituents
            .Select(c => c.SectorEn ?? c.SectorAr)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sectors.Count > 1)
        {
            // Group by sector and normalize each sector group independently
            var groups = constituents.GroupBy(c => (c.SectorEn ?? c.SectorAr ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase);
            foreach (var g in groups)
            {
                var groupWeights = g.Where(c => c.Weight.HasValue && c.Weight.Value > 0).ToList();
                if (groupWeights.Count == 0) continue;

                var groupSum = groupWeights.Sum(c => c.Weight!.Value);
                // If this sector group sums to about 1 (0.90 to 1.10), it was in fractions
                if (groupSum >= 0.90m && groupSum <= 1.10m)
                {
                    foreach (var item in groupWeights)
                    {
                        item.Weight = item.Weight!.Value * 100m;
                    }
                }
            }
            return;
        }

        // Single index column
        var totalSum = itemsWithWeight.Sum(c => c.Weight!.Value);
        if (totalSum >= 0.90m && totalSum <= 1.10m)
        {
            foreach (var item in itemsWithWeight)
            {
                item.Weight = item.Weight!.Value * 100m;
            }
        }
    }

    private static decimal ParseDecimal(string text)
    {
        var w = ParseTextWeight(text);
        return w ?? 0m;
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var stripped = Regex.Replace(input, @"<[^>]*>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(stripped).Trim();
    }
}
