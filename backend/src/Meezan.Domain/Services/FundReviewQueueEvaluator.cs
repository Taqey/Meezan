using Meezan.Domain.Entities;
using Meezan.Domain.Models;

namespace Meezan.Domain.Services;

public static class FundReviewQueueEvaluator
{
    public const string UnclassifiedSlug = "equity-unclassified";

    public static ReviewQueue BuildQueue(
        IEnumerable<Fund> activeFunds,
        IReadOnlyDictionary<int, EffectiveClassification> effectiveMap,
        ClassificationContext ctx)
    {
        var byId = activeFunds.ToDictionary(f => f.Id);
        var q = new ReviewQueue();

        foreach (var (fundId, e) in effectiveMap)
        {
            if (!byId.TryGetValue(fundId, out var f)) continue;
            bool isManual = string.Equals(e.Source, "Manual", StringComparison.OrdinalIgnoreCase);
            var row = new ReviewRow
            {
                FundId = f.Id,
                Code = f.Code,
                NameAr = f.NameAr,
                NameEn = f.NameEn,
                AssetManagerName = f.AssetManagerName,
                Description = f.DescriptionAr,
                InvestmentStrategy = f.InvestmentStrategy,
                Reason = e.Reason,
                Path = e.DisplayPathAr,
                Source = e.Source,
                IsManual = isManual
            };

            if (string.Equals(e.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase))
                q.UnclassifiedEquity.Add(row);

            if (e.NeedsReview && e.ReviewReasons.Any(r => r.Contains("متضاربة") || r.Contains("مؤشر SNDUK") || r.Contains("مختلف")))
                q.Conflicting.Add(row);
            else if (e.NeedsReview && e.ReviewReasons.Any())
            {
                if (!string.Equals(e.Slug, UnclassifiedSlug, StringComparison.OrdinalIgnoreCase))
                    q.MissingChild.Add(row);
            }
            else if (e.NeedsReview && string.Equals(e.Source, "SNDUK", StringComparison.OrdinalIgnoreCase))
            {
                if (ctx.BySlug.TryGetValue(e.Slug, out var n) && n.RequiresChildSelection)
                    q.MissingChild.Add(row);
            }
        }

        q.UnclassifiedEquity = q.UnclassifiedEquity.OrderBy(r => r.Code).ToList();
        q.Conflicting = q.Conflicting.OrderBy(r => r.Code).ToList();
        q.MissingChild = q.MissingChild.OrderBy(r => r.Code).ToList();
        return q;
    }
}
