using ClosedXML.Excel;
using Meezan.Application.Common.Services.Text;

namespace Meezan.Infrastructure.Tests.Features.Stocks;

/// <summary>
/// Covers the changes introduced to support "name-only" stocks (no ISIN / Reuters code):
///  - Arabic-name normalisation for duplicate detection
///  - Parser correctly passes through rows that have only an Arabic name
///  - TOTAL rows and blank rows are dropped
///  - Weights are passed through as-is (decimal fraction, e.g. 0.35 = 35%)
///  - ComputeTicker returns empty string for rows with no code at all
/// </summary>
public class ExcelParserWeightTests
{
    // ── ArabicTextNormalizer ─────────────────────────────────────────────────

    [Theory]
    [InlineData("غاز مصر", "غازمصر")]
    [InlineData("طاقة عربية", "طاقهعربيه")] // ة→ه
    [InlineData("إيبكو", "ايبكو")]       // إ→ا
    [InlineData("آمون", "امون")]        // آ→ا
    [InlineData("أوراسكوم", "اوراسكوم")]   // أ→ا
    [InlineData("إسكو", "اسكو")]        // إ→ا
    [InlineData("المصريه للاتصالات", "المصريهللاتصالات")] // spaces stripped
    public void Normalize_MapsVariantsCorrectly(string input, string expected)
    {
        Assert.Equal(expected, ArabicTextNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_TwoNamesThatDifferOnlyInHamza_AreEqual()
    {
        // "أوراق" vs "اوراق" — both should normalize to same key
        Assert.Equal(
            ArabicTextNormalizer.Normalize("أوراق"),
            ArabicTextNormalizer.Normalize("اوراق"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_NullOrWhiteSpace_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, ArabicTextNormalizer.Normalize(input));
    }

    // ── Parser: rows with only an Arabic name ───────────────────────────────

    [Fact]
    public async Task ParseExcel_RowWithOnlyArabicName_IsNotDropped()
    {
        // Arrange: build an in-memory .xlsx with one Arabic-name-only data row.
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        // Header row
        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 3).Value = "SYMB_NAME_ENG";
        ws.Cell(1, 4).Value = "REUTERS_CODE";
        ws.Cell(1, 5).Value = "SECTOR_ARB";
        ws.Cell(1, 6).Value = "SECTOR_ENG";
        ws.Cell(1, 7).Value = "Weight 01/09/2026";

        // Data row: ISIN/Reuters empty, Arabic name present
        ws.Cell(2, 1).Value = ""; // SYMBOL_CODE
        ws.Cell(2, 2).Value = "غاز مصر";
        ws.Cell(2, 3).Value = ""; // English name
        ws.Cell(2, 4).Value = ""; // Reuters
        ws.Cell(2, 5).Value = "مرافق";
        ws.Cell(2, 6).Value = "Utilities";
        ws.Cell(2, 7).Value = 0.35;

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();

        // Act
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx", CancellationToken.None);

        // Assert
        Assert.Single(rows);
        var row = rows[0];
        Assert.Equal("غاز مصر", row.NameAr);
        Assert.Equal("مرافق", row.SectorAr);
        Assert.Equal("Utilities", row.SectorEn);
        Assert.Equal(0.35m, row.Weight);
        // Ticker should be empty (no Reuters / SymbolCode supplied)
        Assert.Equal(string.Empty, row.Ticker);
    }

    [Fact]
    public async Task ParseExcel_RowWithReutersCode_YieldsTicker()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 3).Value = "REUTERS_CODE";
        ws.Cell(1, 4).Value = "Weight";

        ws.Cell(2, 1).Value = "EG1234";
        ws.Cell(2, 2).Value = "شركة ما";
        ws.Cell(2, 3).Value = "AMOC.CA";
        ws.Cell(2, 4).Value = 0.05;

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx");

        Assert.Single(rows);
        Assert.Equal("AMOC", rows[0].Ticker);
        Assert.Equal(0.05m, rows[0].Weight);
    }

    // ── Parser: TOTAL rows are skipped ──────────────────────────────────────

    [Fact]
    public async Task ParseExcel_TotalRow_IsSkipped()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 3).Value = "REUTERS_CODE";
        ws.Cell(1, 4).Value = "Weight";

        // Real row
        ws.Cell(2, 2).Value = "شركة ما";
        ws.Cell(2, 3).Value = "AMOC.CA";
        ws.Cell(2, 4).Value = 0.05;

        // TOTAL row — only weight filled (name empty)
        ws.Cell(3, 4).Value = 1.0;

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx");

        // Only the real stock row should come through
        Assert.Single(rows);
        Assert.Equal("AMOC", rows[0].Ticker);
    }

    [Fact]
    public async Task ParseExcel_ExplicitMajmooRow_IsSkipped()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 4).Value = "Weight";

        ws.Cell(2, 2).Value = "شركة ما";
        ws.Cell(2, 4).Value = 0.05;

        // الإجمالي row
        ws.Cell(3, 2).Value = "الإجمالي";
        ws.Cell(3, 4).Value = 1.0;

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx");

        Assert.Single(rows);
    }

    // ── Parser: multiple Arabic-name-only rows in one file ──────────────────

    [Fact]
    public async Task ParseExcel_TwoArabicNameOnlyRows_BothReturned()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 5).Value = "SECTOR_ARB";
        ws.Cell(1, 6).Value = "SECTOR_ENG";
        ws.Cell(1, 7).Value = "Weight";

        ws.Cell(2, 2).Value = "غاز مصر";
        ws.Cell(2, 5).Value = "مرافق";
        ws.Cell(2, 6).Value = "Utilities";
        ws.Cell(2, 7).Value = 0.60;

        ws.Cell(3, 2).Value = "طاقة عربية";
        ws.Cell(3, 5).Value = "مرافق";
        ws.Cell(3, 6).Value = "Utilities";
        ws.Cell(3, 7).Value = 0.40;

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx");

        Assert.Equal(2, rows.Count);
        var names = rows.Select(r => r.NameAr).ToHashSet();
        Assert.Contains("غاز مصر", names);
        Assert.Contains("طاقة عربية", names);
    }

    // ── ComputeTicker static helper ──────────────────────────────────────────

    [Theory]
    [InlineData("AMOC.CA", "", "AMOC")]
    [InlineData("NDRL.CA", "", "NDRL")]
    [InlineData("EGAS", "", "EGAS")]
    [InlineData("", "EG123", "EG123")]
    [InlineData(null, "", "")]       // both empty → empty
    [InlineData(null, null, "")]       // both null → empty
    public void ComputeTicker_Various(string? reuters, string? symbolCode, string expected)
    {
        var result = ExcelParserService.ComputeTicker(reuters, symbolCode ?? string.Empty);
        Assert.Equal(expected, result);
    }

    // ── Weight parsing & normalization tests ─────────────────────────────

    [Theory]
    [InlineData("35%", 35.0)]
    [InlineData("35 %", 35.0)]
    [InlineData("35", 35.0)]
    [InlineData("35.5", 35.5)]
    [InlineData("35,5", 35.5)]
    [InlineData("0.35%", 0.35)]
    [InlineData("0,35", 0.35)]
    public void ParseTextWeight_ParsesFormatsCorrectly(string input, decimal expected)
    {
        var result = ExcelParserService.ParseTextWeight(input);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task ParseExcel_PercentFormattedCell_ConvertsFractionToPercentagePoints()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 7).Value = "Weight";

        ws.Cell(2, 1).Value = "EGAS";
        ws.Cell(2, 2).Value = "غاز مصر";
        ws.Cell(2, 7).Value = 0.35;
        ws.Cell(2, 7).Style.NumberFormat.Format = "0.00%";

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx");

        Assert.Single(rows);
        Assert.Equal(35.0m, rows[0].Weight);
    }

    [Fact]
    public async Task ParseExcel_ColumnSummingToOne_MultipliesBy100()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 7).Value = "Weight";

        ws.Cell(2, 1).Value = "EGAS";
        ws.Cell(2, 2).Value = "غاز مصر";
        ws.Cell(2, 7).Value = 0.60;

        ws.Cell(3, 1).Value = "TAQA";
        ws.Cell(3, 2).Value = "طاقة عربية";
        ws.Cell(3, 7).Value = 0.40;

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx");

        Assert.Equal(2, rows.Count);
        // Column summed to 1.0 -> converted to percentage points (60 and 40)
        Assert.Equal(60.0m, rows[0].Weight);
        Assert.Equal(40.0m, rows[1].Weight);
    }

    [Fact]
    public async Task ParseExcel_ColumnSummingTo100_KeepsValuesAsAre()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sheet1");

        ws.Cell(1, 1).Value = "SYMBOL_CODE";
        ws.Cell(1, 2).Value = "SYMB_NAME_ARB";
        ws.Cell(1, 7).Value = "Weight";

        ws.Cell(2, 1).Value = "EGAS";
        ws.Cell(2, 2).Value = "غاز مصر";
        ws.Cell(2, 7).Value = 60.0;

        ws.Cell(3, 1).Value = "TAQA";
        ws.Cell(3, 2).Value = "طاقة عربية";
        ws.Cell(3, 7).Value = 40.0;

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var parser = new ExcelParserService();
        var rows = await parser.ParseIndexConstituentsAsync(ms, "test.xlsx");

        Assert.Equal(2, rows.Count);
        Assert.Equal(60.0m, rows[0].Weight);
        Assert.Equal(40.0m, rows[1].Weight);
    }

    // ── Normalizer idempotency: same input → same output every call ──────────

    [Fact]
    public void Normalize_IsIdempotent()
    {
        var input = "أوراق ومواد تعبئة وتغليف";
        var first = ArabicTextNormalizer.Normalize(input);
        var second = ArabicTextNormalizer.Normalize(first);
        Assert.Equal(first, second);
    }
}
