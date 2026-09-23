namespace Meezan.Domain.Enums;

public enum PriceComparison
{
    Cheap = 1,      // "أصغر" - fair_value > price * 1.05
    Expensive = 2,  // "أكبر" - fair_value < price * 0.95
    Fair = 3        // "تقريبًا قدها" - within +/- 5% range
}
