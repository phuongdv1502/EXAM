using AIQuizHSG.Application.Exams;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Exams;
using AIQuizHSG.Domain.Omr;

namespace AIQuizHSG.Tests;

public class ExamVariantTests
{
    [Fact]
    public void Parse_BuildsSequentialCodes_WhenTextBlank()
    {
        Assert.True(ExamVariantCodes.TryParse(null, 3, out var codes, out _));
        Assert.Equal(["001", "002", "003"], codes);
    }

    [Fact]
    public void Parse_ExpandsRange_WhenDashProvided()
    {
        Assert.True(ExamVariantCodes.TryParse("034-036", 3, out var codes, out _));
        Assert.Equal(["034", "035", "036"], codes);
    }

    [Fact]
    public void Build_KeepsStableOrder_ForSameSeed()
    {
        var exam = SampleExam();
        var first = ExamVariantLayout.Build(exam, "101", 1, 2, DateTime.UtcNow);
        var again = ExamVariantLayout.Build(exam, "101", 1, 2, DateTime.UtcNow);
        Assert.Equal(first.QuestionOrderJson, again.QuestionOrderJson);
        Assert.Equal(first.OptionOrderJson, again.OptionOrderJson);
    }

    [Fact]
    public void Build_ShufflesDifferently_AcrossVariantIndexes()
    {
        var exam = SampleExam();
        var a = ExamVariantLayout.Build(exam, "101", 1, 2, DateTime.UtcNow);
        var b = ExamVariantLayout.Build(exam, "102", 2, 2, DateTime.UtcNow);
        Assert.NotEqual(a.OptionOrderJson, b.OptionOrderJson);
    }

    [Fact]
    public void OptionIdAt_MapsBubbleIndex_ToVariantOption()
    {
        var exam = SampleExam();
        var variant = ExamVariantLayout.Build(exam, "101", 1, 2, DateTime.UtcNow);
        var questionId = ExamVariantLayout.QuestionIds(variant)[0];
        var options = ExamVariantLayout.OptionIds(variant)[questionId];
        Assert.Equal(options[0], ExamVariantLayout.OptionIdAt(variant, questionId, 0));
        Assert.Equal(options[1], ExamVariantLayout.OptionIdAt(variant, questionId, 1));
    }

    [Fact]
    public void SameExamCode_MatchesLegacyAndDigits()
    {
        Assert.True(OmrRules.SameExamCode("DE12", "12"));
        Assert.True(OmrRules.SameExamCode("012", "12"));
        Assert.False(OmrRules.SameExamCode("101", "102"));
    }

    private static Exam SampleExam()
    {
        var exam = new Exam { Id = 9, Title = "Demo" };
        for (var q = 1; q <= 3; q++)
        {
            var question = new ExamQuestion
            {
                Id = q,
                ExamId = exam.Id,
                SortOrder = q,
                ContentSnapshot = $"Câu {q}",
                Type = QuestionType.SingleChoice,
                Points = 1
            };
            for (var o = 1; o <= 4; o++)
            {
                question.Options.Add(new ExamOption
                {
                    Id = q * 10 + o,
                    ExamQuestionId = q,
                    SortOrder = o,
                    ContentSnapshot = $"PA{o}",
                    IsCorrect = o == 1
                });
            }

            exam.Questions.Add(question);
        }

        return exam;
    }
}
