using Meezan.Domain.Entities;

namespace Meezan.Application.Common;

/// <summary>
/// Single source of truth for "this stock is overseen by a Shariah board/committee".
/// A plain "لجنة شرعية" and an accredited "هيئة رقابة شرعية داخلية معتمدة" are the same
/// case: both are detected here and rendered identically (status + one unified note),
/// with the purification percentage, the 7-source opinion panel and the AAOIFI/S&amp;P
/// metrics panel suppressed.
/// </summary>
public static class ShariahBoardDetector
{
    /// <summary>
    /// Unified note for every board-governed stock. It deliberately does not distinguish
    /// board type (internal vs plain committee).
    /// </summary>
    public const string BoardNote =
        "تشرف لجنة/هيئة شرعية على هذا السهم وعلى توافق أنشطته ومعاييره الشرعية.";

    /// <summary>True when a source/seed note indicates board governance.</summary>
    public static bool IsBoardNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return false;

        // "لجنة شرعية"، "لجنة الرقابة الشرعية"، "لجنة الشئون الشرعية" …
        if (note.Contains("لجنة") && note.Contains("شرعية")) return true;

        // "هيئة رقابة شرعية داخلية معتمدة …"
        if (note.Contains("هيئة رقابة شرعية")) return true;

        return false;
    }

    /// <summary>
    /// Board governance = the persisted flag on <see cref="ShariahCompliance"/> (populated
    /// from seed/source data) or, as a safety net, any source-opinion note that matches.
    /// </summary>
    public static bool IsBoardGoverned(
        ShariahCompliance? compliance,
        IEnumerable<ShariahSourceOpinion>? opinions)
        => IsBoardGoverned(compliance?.HasShariahBoard == true, Notes(opinions));

    public static bool IsBoardGoverned(bool hasFlag, IEnumerable<string?>? notes)
        => hasFlag || (notes?.Any(IsBoardNote) ?? false);

    private static IEnumerable<string?> Notes(IEnumerable<ShariahSourceOpinion>? opinions)
        => opinions?.Select(o => o.Note) ?? Enumerable.Empty<string?>();

    /// <summary>The unified note when board-governed, otherwise null.</summary>
    public static string? NoteFor(bool hasBoard) => hasBoard ? BoardNote : null;
}
