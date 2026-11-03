using System.Text.Json;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Exams;

namespace AIQuizHSG.Application.Exams;

// Khóa thứ tự câu và phương án của một mã đề để in phiếu và chấm OMR.
public static class ExamVariantLayout
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // Nhiều mã thì mỗi mã xáo theo seed riêng. Một mã thì giữ thứ tự gốc của đề.
    public static ExamVariant Build(Exam exam, string code, int sortOrder, int variantCount, DateTime now)
    {
        var shuffle = variantCount > 1;
        var questions = ExamOrder.Arrange(
            exam.Questions,
            shuffle,
            ExamOrder.Seed(exam.Id, sortOrder),
            item => item.SortOrder,
            item => item.Id);

        var questionIds = questions.Select(item => item.Id).ToList();
        var optionMap = new Dictionary<string, long[]>();
        foreach (var question in questions)
        {
            var options = ExamOrder.Arrange(
                question.Options,
                shuffle,
                ExamOrder.Seed(exam.Id, question.Id * 1000L + sortOrder),
                item => item.SortOrder,
                item => item.Id);
            optionMap[question.Id.ToString()] = options.Select(item => item.Id).ToArray();
        }

        return new ExamVariant
        {
            ExamId = exam.Id,
            Code = code,
            SortOrder = sortOrder,
            QuestionOrderJson = JsonSerializer.Serialize(questionIds, JsonOptions),
            OptionOrderJson = JsonSerializer.Serialize(optionMap, JsonOptions),
            CreatedAtUtc = now
        };
    }

    public static IReadOnlyList<long> QuestionIds(ExamVariant variant) =>
        JsonSerializer.Deserialize<List<long>>(variant.QuestionOrderJson, JsonOptions) ?? [];

    public static IReadOnlyDictionary<long, IReadOnlyList<long>> OptionIds(ExamVariant variant)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, long[]>>(variant.OptionOrderJson, JsonOptions)
            ?? new Dictionary<string, long[]>();
        return raw.ToDictionary(
            item => long.Parse(item.Key),
            item => (IReadOnlyList<long>)item.Value.ToList());
    }

    // ChoiceIndex trên phiếu là vị trí phương án theo mã đề, đổi sang Id phương án gốc.
    public static long? OptionIdAt(ExamVariant variant, long questionId, int choiceIndex)
    {
        if (!OptionIds(variant).TryGetValue(questionId, out var options))
            return null;
        if (choiceIndex < 0 || choiceIndex >= options.Count)
            return null;
        return options[choiceIndex];
    }
}
