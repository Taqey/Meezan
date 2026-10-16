namespace Meezan.Domain.Models;

public record EffectiveFundTypeResult(
    int? TypeId,
    string Slug,
    string NameAr,
    string NameEn,
    string Source, // SNDUK | يدوي | تلقائي - يتبع مؤشر
    string? OriginalSndukSlug,
    string? OriginalSndukNameAr
);
