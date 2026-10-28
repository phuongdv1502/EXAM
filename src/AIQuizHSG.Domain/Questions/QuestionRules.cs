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

        return null;
    }
}
