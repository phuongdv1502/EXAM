using System.Globalization;
using System.Text;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Questions;

// So khớp tên hoặc mã mức độ, bỏ dấu và khoảng trắng.
public static class DifficultyText
{
    public static bool Matches(DifficultyLevel level, string text)
    {
        var key = Fold(text);
        return key.Length > 0 && (key == Fold(level.Code) || key == Fold(level.Name));
    }

    public static string Fold(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var chars = decomposed.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark
            && !char.IsWhiteSpace(character)
            && character != '/');
        return new string(chars.ToArray());
    }
}
