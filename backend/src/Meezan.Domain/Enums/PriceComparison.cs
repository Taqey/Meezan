namespace Meezan.Domain.Enums;

public enum PriceComparison
{
    Cheap = 1,      // "أصغر" - fair_value > price * 1.05
    Expensive = 2,  // "أكبر" - fair_value < price * 0.95
    Fair = 3,       // "تقريبًا قدها" - within +/- 5% range

    /// <summary>
    /// "لا يمكن حساب القيمة العادلة" — no trustworthy fair value could be produced:
    /// zero applicable methods, nothing survived the all-outliers fallback, or the
    /// stock's own current price is missing/zero/invalid. FairValue, FairValueDiff and
    /// FairValueDiffPct are null in this state and no Cheap/Expensive/Fair verdict exists.
    /// </summary>
    Unavailable = 4
}
