namespace Meezan.Domain.Services;

public static class FundSlugValidator
{
    public static (bool Ok, string? Error) Validate(string slug)
    {
        slug = (slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug)) return (false, "المعرف (slug) مطلوب.");
        if (slug.Length > 100) return (false, "المعرف طويل جداً (بحد أقصى 100).");
        foreach (var ch in slug)
        {
            if (!(ch >= 'a' && ch <= 'z') && !(ch >= '0' && ch <= '9') && ch != '-' && ch != '_')
                return (false, "المعرف يقبل أحرفاً لاتينية صغيرة وأرقاماً و - و _ فقط.");
        }
        return (true, null);
    }
}
