using System.Net;
using System.Text;
using Meezan.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Meezan.Infrastructure.Tests.Features.Funds;

/// <summary>
/// Verifies the SNDUK tRPC batch-array parsing through the REAL <see cref="SndukFundClient"/>
/// parsing path. Only the <see cref="HttpMessageHandler"/> is faked; the client itself is real.
/// </summary>
public class SndukFundClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private sealed class NoopLogger : ILogger<SndukFundClient>
    {
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        bool ILogger.IsEnabled(LogLevel logLevel) => false;
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    private static SndukFundClient CreateClient(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHandler(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(handler);
        return new SndukFundClient(http, new NoopLogger());
    }

    [Fact]
    public async Task FundsList_ParsesTwoFunds_FromBatchArray()
    {
        var body = """
            [{"result":{"data":{"json":{"funds":[
              {"id":101,"publicId":"P101","slug":"fund-a","name":"صندوق أ","nameEn":"Fund A","code":"FA","type":"equity","typeNameAr":"أسهم","typeNameEn":"Equity","riskLevel":"medium","isIslamic":true,"isFeatured":false,"isActive":true,"currentPrice":"12.5","dailyChange":"0.5","returns":{"1M":"1.0","3M":null,"6M":"3.0","1Y":"8.5","3Y":"20.0"},"totalScore":"75.5","aum":"1500000.50","managementFee":"1.25","minimumInvestment":"1000 EGP","currency":"EGP","assetManagerId":7,"assetManagerName":"مدير","assetManagerNameEn":"Manager","assetManagerCode":"MGR","logoUrl":"https://x/logo.png","description":"وصف","descriptionEn":"desc","investmentStrategy":"strat","inceptionDate":"2020-01-01","prospectusUrl":null,"createdAt":"2020-01-01","updatedAt":"2024-01-01","someUnknownFutureField":"ignore-me"},
              {"ID":102,"publicId":"P102","slug":"fund-b","name":"صندوق ب","nameEn":null,"code":"FB","type":null,"typeNameAr":null,"typeNameEn":null,"riskLevel":null,"isIslamic":false,"isFeatured":null,"isActive":true,"currentPrice":null,"dailyChange":null,"returns":null,"totalScore":null,"aum":null,"managementFee":null,"minimumInvestment":null,"currency":null,"assetManagerId":null,"assetManagerName":null,"assetManagerNameEn":null,"assetManagerCode":null,"logoUrl":null,"description":null,"descriptionEn":null,"investmentStrategy":null,"inceptionDate":null,"prospectusUrl":null,"createdAt":null,"updatedAt":null}
            ]}}}}]
            """;

        var client = CreateClient(body);
        var result = await client.GetFundListAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Funds.Length);
        Assert.Equal(101, result.Funds[0].Id);
        Assert.Equal("FA", result.Funds[0].Code);
        Assert.Equal("1500000.50", result.Funds[0].Aum);
        Assert.Equal("1.25", result.Funds[0].ManagementFee);
        Assert.Equal("8.5", result.Funds[0].Returns!.Y1);
        Assert.Equal(102, result.Funds[1].Id);
        Assert.Null(result.Funds[1].Returns);
        Assert.Null(result.Funds[1].Aum);
    }

    [Fact]
    public async Task Performance_ParsesDataPoints_FromBatchArray()
    {
        var body = """
            [{"result":{"data":{"json":{"data":[
              {"date":"2024-01-01","price":"10.0","change":"0.1","performance":"0.5"},
              {"date":"2024-02-01","price":"10.5","change":"0.2","performance":"1.0"},
              {"date":"2024-03-01","price":"11.0","change":"0.3","performance":"1.5"}
            ],"summary":{"totalReturn":"10.0","volatility":"5.0","maxDrawdown":"-2.0","sharpeRatio":"1.2"}}}}}]
            """;

        var client = CreateClient(body);
        var result = await client.GetFundPerformanceAsync(101, "1Y");

        Assert.NotNull(result);
        Assert.Equal(3, result!.Data.Length);
        Assert.Equal("2024-01-01", result.Data[0].Date);
        Assert.Equal("11.0", result.Data[2].Price);
        Assert.Equal("10.0", result.Summary!.TotalReturn);

        var dates = result.Data.Select(p => DateTime.Parse(p.Date!)).ToArray();
        Assert.True(dates[0] < dates[1] && dates[1] < dates[2]);
        Assert.All(result.Data, p => Assert.True(decimal.Parse(p.Price!) > 0));
        Assert.Equal(3, result.Data.Select(p => p.Date).Distinct().Count());
    }

    [Fact]
    public async Task Analysis_ParsesScores_IncludingMinusOne_FromBatchArray()
    {
        var body = """
            [{"result":{"data":{"json":{"scores":{"performance":80.5,"risk":-1,"cost":70.0,"stability":null,"diversification":60.0,"total":75.0},"rawMetrics":{"cagr":8.5,"alpha":1.2,"volatility":5.0,"consistency":90.0,"maxDrawdown":-3.0,"sharpeRatio":1.1},"weights":{"performance":0.35,"risk":0.25,"cost":0.15,"stability":0.15,"diversification":0.1},"explanations":{"performance":{"ar":["أداء قوي"],"en":["Strong"]},"risk":{"ar":[],"en":[]},"cost":{"ar":["تكلفة منخفضة"],"en":[]},"stability":{"ar":[],"en":[]},"diversification":{"ar":[],"en":[]}},"calculatedAt":"2024-06-01"}}}}] 
            """;

        var client = CreateClient(body);
        var result = await client.GetFundAnalysisAsync(101);

        Assert.NotNull(result);
        Assert.Equal(80.5m, result!.Scores!.Performance);
        Assert.Equal(-1m, result.Scores!.Risk);
        Assert.Null(result.Scores!.Stability);
        Assert.Equal(8.5m, result.RawMetrics!.Cagr);
        Assert.Equal("أداء قوي", result.Explanations!.Performance!.Ar!.Single());
        Assert.Equal(0.35m, result.Weights!.Value.GetProperty("performance").GetDecimal());
    }

    [Fact]
    public async Task Competing_ParsesFundList_FromBatchArray()
    {
        var body = """
            [{"result":{"data":{"json":{"funds":[
              {"id":201,"publicId":"P201","slug":"fund-c","name":"صندوق ج","nameEn":"Fund C","code":"FC","type":"equity","typeNameAr":"أسهم","typeNameEn":"Equity","riskLevel":"high","isIslamic":true,"isFeatured":false,"isActive":true,"currentPrice":"20.0","dailyChange":"-0.5","returns":null,"totalScore":null,"aum":"500000","managementFee":"2.0","minimumInvestment":null,"currency":"EGP","assetManagerId":null,"assetManagerName":null,"assetManagerNameEn":null,"assetManagerCode":null,"logoUrl":null,"description":null,"descriptionEn":null,"investmentStrategy":null,"inceptionDate":null,"prospectusUrl":null,"createdAt":null,"updatedAt":null},
              {"id":202,"publicId":"P202","slug":"fund-d","name":"صندوق د","nameEn":"Fund D","code":"FD","type":"index","typeNameAr":"مؤشر","typeNameEn":"Index","riskLevel":"low","isIslamic":false,"isFeatured":true,"isActive":true,"currentPrice":"30.0","dailyChange":"0.1","returns":null,"totalScore":null,"aum":null,"managementFee":null,"minimumInvestment":null,"currency":"EGP","assetManagerId":null,"assetManagerName":null,"assetManagerNameEn":null,"assetManagerCode":null,"logoUrl":null,"description":null,"descriptionEn":null,"investmentStrategy":null,"inceptionDate":null,"prospectusUrl":null,"createdAt":null,"updatedAt":null}
            ]}}}}]
            """;

        var client = CreateClient(body);
        var result = await client.GetFundCompetingAsync(101);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Funds.Length);
        Assert.Equal("FC", result.Funds[0].Code);
        Assert.Equal("FD", result.Funds[1].Code);
    }

    [Fact]
    public async Task FundsList_AcceptsNumericAndStringScalars()
    {
        // Regression test for the live failure:
        // "The JSON value could not be converted to System.String. Path: $.funds[0].currentPrice".
        // SNDUK sends the same fields as raw numbers for some funds and as strings for others.
        var body = """
            [{"result":{"data":{"json":{"funds":[
              {"id":54,"publicId":"P54","slug":"fund-x","name":"صندوق س","nameEn":"Fund X","code":"PHF","type":"equity","typeNameAr":"أسهم","typeNameEn":"Equity","riskLevel":"medium","isIslamic":true,"isFeatured":false,"isActive":true,"currentPrice":12.5,"dailyChange":-0.25,"returns":{"1M":1.5,"3M":null,"6M":4.0,"1Y":9.75,"3Y":null},"totalScore":80,"aum":2500000,"managementFee":1.5,"minimumInvestment":1000,"currency":"EGP","assetManagerId":"7","assetManagerName":"مدير","assetManagerNameEn":"Manager","assetManagerCode":"MGR","logoUrl":null,"description":null,"descriptionEn":null,"investmentStrategy":null,"inceptionDate":null,"prospectusUrl":null,"createdAt":null,"updatedAt":null}
            ]}}}}]
            """;

        var client = CreateClient(body);
        var result = await client.GetFundListAsync();

        Assert.NotNull(result);
        var fund = Assert.Single(result!.Funds);
        Assert.Equal("12.5", fund.CurrentPrice);
        Assert.Equal("-0.25", fund.DailyChange);
        Assert.Equal("1.5", fund.Returns!.M1);
        Assert.Equal("9.75", fund.Returns!.Y1);
        Assert.Equal("80", fund.TotalScore);
        Assert.Equal("2500000", fund.Aum);
        Assert.Equal("1.5", fund.ManagementFee);
        Assert.Equal("1000", fund.MinimumInvestment);
        Assert.Equal(7, fund.AssetManagerId);
    }

    [Fact]
    public async Task Requests_UseTrpcBatchWrapper()
    {
        // Regression test: per-fund calls without the {"0":...} batch wrapper get
        // HTTP 400 "Invalid input: expected object, received undefined" from SNDUK,
        // and funds.list without it silently returns only a page of funds.
        HttpRequestMessage? captured = null;
        var handler = new FakeHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """[{"result":{"data":{"json":{"data":[],"summary":null}}}}]""",
                    Encoding.UTF8, "application/json")
            };
        });
        var client = new SndukFundClient(new HttpClient(handler), new NoopLogger());

        await client.GetFundPerformanceAsync(54, "1Y");

        Assert.NotNull(captured);
        var query = captured!.RequestUri!.Query;
        Assert.Contains("batch=1", query);
        var marker = "input=";
        var start = query.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        var input = Uri.UnescapeDataString(query.Substring(start + marker.Length));
        using var doc = System.Text.Json.JsonDocument.Parse(input);
        Assert.True(doc.RootElement.TryGetProperty("0", out var first));
        Assert.True(first.TryGetProperty("json", out _));
    }

    [Fact]
    public async Task ErrorResponse_ThrowsClearException()
    {
        var body = """[{"error":{"message":"Fund not found","code":-32004}}]""";
        var client = CreateClient(body);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetFundListAsync());
        Assert.Contains("Fund not found", ex.Message);
        Assert.Contains("funds.list", ex.Message);
    }
}

