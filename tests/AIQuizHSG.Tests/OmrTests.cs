using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Omr;
using AIQuizHSG.Infrastructure.Omr;

namespace AIQuizHSG.Tests;

public class OmrTests
{
    [Fact]
    public void Grade_GivesPointsOnlyWhenTheChosenSetMatches()
    {
        var questions = new List<ExamQuestion>
        {
            Question(1, 1.5m, correct: [11], choices: [11, 12]),
            Question(2, 2m, correct: [21, 22], choices: [21, 22, 23])
        };

        var exact = ExamGrading.Score(questions, id => id == 1 ? [11] : [22, 21]);
        var wrong = ExamGrading.Score(questions, id => id == 1 ? [] : [21]);

        Assert.Equal(3.5m, exact.Score);
        Assert.Equal(2, exact.Correct);
        Assert.Equal(0, exact.Wrong);
        Assert.Equal(0, exact.Blank);
        Assert.Equal(0m, wrong.Score);
        Assert.Equal(1, wrong.Blank);
        Assert.Equal(1, wrong.Wrong);
    }

    [Fact]
    public void Publish_RequiresEverySheetGraded()
    {
        Assert.False(OmrRules.CanApprove([]));
        Assert.False(OmrRules.CanApprove([OmrSheetStatus.Graded, OmrSheetStatus.NeedsReview]));
        Assert.False(OmrRules.CanApprove([OmrSheetStatus.Failed]));
        Assert.True(OmrRules.CanApprove([OmrSheetStatus.Graded, OmrSheetStatus.Graded]));
    }

    [Fact]
    public void Recognize_ReadsQrAndFilledBubbles()
    {
        var config = OmrTemplateConfig.Grid(2, 2);
        var png = OmrSampleImage.Png(config, "HS26001", "DE01", new HashSet<(int, int)> { (1, 0) });
        var read = new OmrRecognitionService().Recognize(png, config);

        Assert.Null(read.Error);
        Assert.True(read.QrFound);
        Assert.Equal("HS26001", read.StudentCode);
        Assert.Equal("DE01", read.ExamCode);
        Assert.Equal(nameof(OmrMarkStatus.Marked), Bubble(read, 1, 0));
        Assert.Equal(nameof(OmrMarkStatus.Blank), Bubble(read, 1, 1));
        Assert.Equal(nameof(OmrMarkStatus.Blank), Bubble(read, 2, 0));
        Assert.Equal(nameof(OmrMarkStatus.Blank), Bubble(read, 2, 1));
    }

    private static string Bubble(OmrRecognitionResult read, int question, int choice) =>
        read.Bubbles.Single(item => item.QuestionNumber == question && item.ChoiceIndex == choice).Status;

    private static ExamQuestion Question(long id, decimal points, IReadOnlyList<long> correct, IReadOnlyList<long> choices) => new()
    {
        Id = id,
        Points = points,
        Type = correct.Count > 1 ? QuestionType.MultipleChoice : QuestionType.SingleChoice,
        Options = choices.Select((choice, index) => new ExamOption
        {
            Id = choice,
            ExamQuestionId = id,
            IsCorrect = correct.Contains(choice),
            SortOrder = index + 1,
            ContentSnapshot = choice.ToString()
        }).ToList()
    };
}
