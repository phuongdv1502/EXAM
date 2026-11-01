using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Questions;

// Quy tắc phương án dùng chung cho form, gửi duyệt và từng dòng Excel.
public static class QuestionRules
{
    public const int MinOptions = 2;
    public const int MaxOptions = 6;

    public static string? ValidateOptions(QuestionType type, IReadOnlyList<(string Content, bool IsCorrect)> options)
    {
        if (options.Count < MinOptions)
            return Messages.NeedAtLeastTwoOptions;
        if (options.Count > MaxOptions)
            return Messages.TooManyOptions;
        if (options.Any(option => string.IsNullOrWhiteSpace(option.Content)))
            return Messages.OptionContentRequired;

        var correctCount = options.Count(option => option.IsCorrect);
        if (correctCount == 0)
            return Messages.NeedOneCorrect;
        if (correctCount == options.Count)
            return Messages.NeedOneWrong;
        if (type == QuestionType.SingleChoice && correctCount != 1)
            return Messages.SingleChoiceOneCorrect;
        // Đúng/sai luôn là Đúng rồi Sai, đúng một đáp án, để khớp ô trên phiếu.
        if (type == QuestionType.TrueFalse
            && (options.Count != 2 || !IsDung(options[0].Content) || !IsSai(options[1].Content) || correctCount != 1))
            return Messages.TrueFalseOptions;
        // Điền số: một giá trị đúng vừa 4 cột phiếu, các phương án còn lại là số sai cùng dạng.
        if (type == QuestionType.FillNumber
            && (correctCount != 1 || options.Any(option => !IsGridNumber(option.Content))))
            return Messages.FillNumberOptions;

        return null;
    }

    // Lưới phiếu có 4 cột: dấu trừ, dấu phẩy và chữ số.
    public static bool IsGridNumber(string? content)
    {
        var text = (content ?? string.Empty).Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace('.', ',');
        if (text.Length is < 1 or > 4)
            return false;

        var comma = false;
        var digit = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '-' && index == 0)
                continue;
            if (character == ',' && !comma && digit)
            {
                comma = true;
                continue;
            }

            if (!char.IsDigit(character))
                return false;
            digit = true;
        }

        return digit && !text.EndsWith(',');
    }

    public static bool IsDung(string? content) => Key(content) is "dung" or "đúng";

    public static bool IsSai(string? content) => Key(content) is "sai";

    private static string Key(string? value) =>
        string.Concat((value ?? string.Empty).Trim().ToLowerInvariant().Where(character => !char.IsWhiteSpace(character)));
}
