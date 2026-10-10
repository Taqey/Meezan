using System.Text;
using System.Text.RegularExpressions;

namespace Meezan.Application.Common;

public static class ArabicTextNormalizer
{
    /// <summary>
    /// Normalizes Arabic text for duplicate matching:
    /// - Strips punctuation, spaces, and diacritics
    /// - Normalizes hamzas: أ, إ, آ, ٱ -> ا
    /// - Normalizes yaa: ى -> ي
    /// - Normalizes taa marbuta: ة -> ه
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var s = text.Trim();

        // Remove diacritics / tashkeel (0x064B to 0x065F)
        s = Regex.Replace(s, @"[\u064B-\u065F\u0670]", string.Empty);

        // Normalize Alef variants: أ, إ, آ, ٱ -> ا
        s = Regex.Replace(s, "[أإآٱ]", "ا");

        // Normalize Yaa: ى -> ي
        s = s.Replace('ى', 'ي');

        // Normalize Taa Marbuta: ة -> ه
        s = s.Replace('ة', 'ه');

        // Strip punctuation, spaces, symbols
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return sb.ToString();
    }
}
