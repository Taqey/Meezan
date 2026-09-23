namespace Meezan.Application.Common;

/// <summary>
/// Normalises a requested page size to the allowed set: 10, 20, 50, 100.
/// Any other value is clamped to the nearest valid entry (rounding up, max 100).
/// Default when null or ≤ 0 is 20.
/// </summary>
public static class PageSizeHelper
{
    private static readonly int[] Allowed = [10, 20, 50, 100];

    public static int Clamp(int? requested)
    {
        if (requested is null or <= 0) return 20;
        foreach (var allowed in Allowed)
            if (requested <= allowed) return allowed;
        return 100;
    }

    public static int ClampPage(int? page) => page is null or < 1 ? 1 : page.Value;
}
