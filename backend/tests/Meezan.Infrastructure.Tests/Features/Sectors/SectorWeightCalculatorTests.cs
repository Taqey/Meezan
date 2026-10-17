using System.Collections.Generic;
using System.Linq;
using Meezan.Application.Common;
using Meezan.Application.Common.Services.Text;
using Meezan.Application.Common.Services.Market;
using Meezan.Application.Common.Services.Shariah;
using Xunit;

namespace Meezan.Infrastructure.Tests.Features.Sectors;

public class SectorWeightCalculatorTests
{
    [Fact]
    public void Calculate_BanksSector_ProducesExpectedCappedWeights()
    {
        // Test on the Banks sector: COMI should come out at 35%, QNBE about 21.9%, ADIB about 10.4%.
        // Let's create realistic mock bank market caps where COMI is the largest and exceeds 35%.
        // E.g.:
        // COMI: 150,000
        // QNBE: 60,000
        // ADIB: 28,500
        // Others: sum to ~50,000
        // Let's test with market caps proportionally matching these weights:
        // Capped value for COMI is 35% of total.
        // If uncapped total = sum(non-capped), new total = uncapped total / (1 - 0.35) = uncapped total / 0.65
        // Then QNBE weight = QNBE / new total = (QNBE / uncapped total) * 65%
        // If QNBE is 21.9%, then QNBE / uncapped total = 21.9 / 65 = ~33.7%
        // ADIB weight = (ADIB / uncapped total) * 65% = 10.4% => ADIB / uncapped total = ~16%
        
        var stocks = new List<SectorStockWeightInput>
        {
            new(1, "COMI", 160000m, "EGP"),
            new(2, "QNBE", 60000m, "EGP"),
            new(3, "ADIB", 28500m, "EGP"),
            new(4, "FAIT", 25000m, "EGP"),
            new(5, "HDBK", 20000m, "EGP"),
            new(6, "SAUD", 15000m, "EGP"),
            new(7, "EGBE", 10000m, "EGP"),
            new(8, "CIEB", 10000m, "EGP"),
            new(9, "ABUK", 5000m, "EGP"),
            new(10, "ALEX", 4500m, "EGP")
        };

        var result = SectorWeightCalculator.Calculate(1, stocks);

        var comi = result.StockWeights.First(w => w.Ticker == "COMI");
        var qnbe = result.StockWeights.First(w => w.Ticker == "QNBE");
        var adib = result.StockWeights.First(w => w.Ticker == "ADIB");

        Assert.True(comi.IsCapped);
        Assert.Equal(35.0m, comi.FinalWeight);
        Assert.True(comi.CappingFactor < 1.0m);

        Assert.False(qnbe.IsCapped);
        Assert.InRange(qnbe.FinalWeight!.Value, 21.0m, 22.5m);

        Assert.False(adib.IsCapped);
        Assert.InRange(adib.FinalWeight!.Value, 10.0m, 11.0m);

        // Sum of all weights must be exactly 100%
        Assert.InRange(result.TotalWeightSum, 99.95m, 100.05m);
        Assert.False(result.HasSumWarning);
    }

    [Fact]
    public void Calculate_MultipleStocksCapped_IteratesUntilConvergence()
    {
        // 2 dominant stocks each with very high market cap
        var stocks = new List<SectorStockWeightInput>
        {
            new(1, "STOCK_A", 100000m, "EGP"),
            new(2, "STOCK_B", 90000m, "EGP"),
            new(3, "STOCK_C", 10000m, "EGP"),
            new(4, "STOCK_D", 10000m, "EGP")
        };

        var result = SectorWeightCalculator.Calculate(2, stocks);

        var a = result.StockWeights.First(w => w.Ticker == "STOCK_A");
        var b = result.StockWeights.First(w => w.Ticker == "STOCK_B");
        var c = result.StockWeights.First(w => w.Ticker == "STOCK_C");
        var d = result.StockWeights.First(w => w.Ticker == "STOCK_D");

        Assert.True(a.IsCapped);
        Assert.Equal(35.0m, a.FinalWeight);

        Assert.True(b.IsCapped);
        Assert.Equal(35.0m, b.FinalWeight);

        // Remaining 30% split equally between C (15%) and D (15%)
        Assert.False(c.IsCapped);
        Assert.Equal(15.0m, c.FinalWeight);

        Assert.False(d.IsCapped);
        Assert.Equal(15.0m, d.FinalWeight);

        Assert.InRange(result.TotalWeightSum, 99.99m, 100.01m);
    }

    [Fact]
    public void Calculate_ForeignCurrencyUsdConvertedToEgp()
    {
        // FAITA in USD with $1,000 market cap vs LocalStock with 52,400 EGP
        // At 52.40 exchange rate, both have equal market cap!
        var stocks = new List<SectorStockWeightInput>
        {
            new(1, "FAITA", 1000m, "USD"),
            new(2, "LOCAL", 52400m, "EGP")
        };

        var result = SectorWeightCalculator.Calculate(1, stocks, usdExchangeRate: 52.40m);

        var faita = result.StockWeights.First(w => w.Ticker == "FAITA");
        var local = result.StockWeights.First(w => w.Ticker == "LOCAL");

        Assert.Equal(52400m, faita.EffectiveMarketCapEgp);
        Assert.Equal(50.0m, faita.FinalWeight);
        Assert.Equal(50.0m, local.FinalWeight);
    }

    [Fact]
    public void Calculate_StockWithoutMarketCap_GetsRemainderOrNull()
    {
        var stocks = new List<SectorStockWeightInput>
        {
            new(1, "VALID_A", 6000m, "EGP"),
            new(2, "VALID_B", 3000m, "EGP"),
            new(3, "VALID_C", 1000m, "EGP"),
            new(4, "NO_CAP", null, null)
        };

        var result = SectorWeightCalculator.Calculate(1, stocks);

        var a = result.StockWeights.First(w => w.Ticker == "VALID_A");
        var b = result.StockWeights.First(w => w.Ticker == "VALID_B");
        var c = result.StockWeights.First(w => w.Ticker == "VALID_C");
        var noCap = result.StockWeights.First(w => w.Ticker == "NO_CAP");

        // VALID_A raw weight is 60% > 35%, so it gets capped at 35%.
        // The remaining 65% is split between VALID_B (3000/4000 = 75% of 65% = 48.75% > 35%, so VALID_B also gets capped at 35%!)
        // VALID_C gets remaining 30%.
        // Total valid stocks sum to 100%.
        // NO_CAP gets null (rendered as "—") because nothing remains from 100%.
        Assert.True(a.IsCapped);
        Assert.Equal(35.0m, a.FinalWeight);
        Assert.True(b.IsCapped);
        Assert.Equal(35.0m, b.FinalWeight);
        Assert.False(c.IsCapped);
        Assert.Equal(30.0m, c.FinalWeight);
        Assert.Null(noCap.FinalWeight);
    }
}
