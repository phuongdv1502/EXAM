using System.Text;
using System.Text.RegularExpressions;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Omr;
using AIQuizHSG.Infrastructure.Omr;
using SkiaSharp;

namespace AIQuizHSG.Tests;

public class OmrTests
{
    [Fact]
    public void Recognize_ReadsFilledChoicesOnPrinterScanAndDeskPhoto()
    {
        Assert.True(OmrTemplateConfig.TryFromCheck([("BonDapAn", 32), ("DungSai", 2)], out var config));
        var printerPath = @"F:\Project\Exam\src\AIQuizHSG.Api\storage\omr\16\66C8A1699543C7D7077E233501E721C9E2AEFAC02CBC73485845C97D6BEC6252.jpg";
        var deskPath = @"F:\Project\Exam\src\AIQuizHSG.Api\storage\omr\9\7C00E227846116958A954DB9A1B84503666377878FD29343B4E7CA5BF7E87D2B.jpg";
        var canonPath = @"F:\Project\Exam\src\AIQuizHSG.Api\storage\omr\14\6EA5D36E992CCD0D590697B1AA7C2A13FA068853D3C04ABCE9469171F4579DE3.jpg";
        if (!File.Exists(printerPath) || !File.Exists(deskPath) || !File.Exists(canonPath))
            return;

        string Marks(string path) => string.Join(" ", new OmrRecognitionService().Recognize(File.ReadAllBytes(path), config)
            .Bubbles.Where(item => item.Ratio >= 0.4)
            .OrderBy(item => item.QuestionNumber)
            .Select(item => $"q{item.QuestionNumber}:{item.ChoiceIndex}"));

        Assert.Equal("q1:1 q2:1 q3:2 q4:3 q5:3 q6:0 q7:0 q8:1 q9:1 q10:2 q18:1 q19:2 q20:3", Marks(printerPath));
        Assert.Equal("q1:2 q2:1", Marks(deskPath));
        Assert.StartsWith("q1:2 q2:1", Marks(canonPath));
    }

    [Fact]
    public void MarkChoice_TakesTheBubbleThatStandsOut()
    {
        var clear = OmrMarkChoice.Choose([0.62, 0.40, 0.14, 0.12]);
        var light = OmrMarkChoice.Choose([0.36, 0.12, 0.11, 0.10]);
        var both = OmrMarkChoice.Choose([0.70, 0.64, 0.11, 0.10]);
        var empty = OmrMarkChoice.Choose([0.22, 0.18, 0.16, 0.15]);
        var close = OmrMarkChoice.Choose([0.50, 0.42, 0.12, 0.11]);

        Assert.Equal(OmrMarkStatus.Marked, clear.Status);
        Assert.Equal(0, clear.Index);
        Assert.Equal(OmrMarkStatus.Marked, light.Status);
        Assert.Equal(0, light.Index);
        Assert.Equal(OmrMarkStatus.Ambiguous, both.Status);
        Assert.Equal(OmrMarkStatus.Blank, empty.Status);
        Assert.Equal(OmrMarkStatus.Ambiguous, close.Status);
    }

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
    public void MultiPart_BuildsFourChoiceThenTrueFalseThenDigits()
    {
        var ok = OmrTemplateConfig.TryFromParts(
            [("BonDapAn", 10), ("DungSai", 2), ("DienSo", 1)],
            out var config);

        Assert.True(ok);
        Assert.Equal(10 + 8 + 1, config.QuestionCount);
        Assert.Equal(4, config.Bubbles.Count(item => item.QuestionNumber == 1));
        Assert.Equal(2, config.Bubbles.Count(item => item.QuestionNumber == 11));
        Assert.Equal(OmrSectionKinds.EssayDigitBubbles, config.Bubbles.Count(item => item.QuestionNumber == 19));
        Assert.True(config.Bubbles.Where(item => item.QuestionNumber <= 10).Max(item => item.Y) < config.Bubbles.Where(item => item.QuestionNumber == 11).Min(item => item.Y));
        Assert.True(config.Bubbles.Max(item => item.Y + item.Height) <= 1);
        Assert.True(OmrTemplateConfig.TryParse(config.ToJson(), out var roundTrip));
        Assert.Equal(config.Bubbles.Count, roundTrip.Bubbles.Count);
        Assert.True(config.OptionCountsFit([..Enumerable.Repeat(4, 10), ..Enumerable.Repeat(2, 8), 2]));
        Assert.False(config.OptionCountsFit(Enumerable.Repeat(4, 19).ToList()));
        Assert.True(config.Accepts(Enumerable.Repeat(4, 10).ToList(), exact: false));
        Assert.False(config.Accepts(Enumerable.Repeat(4, 10).ToList(), exact: true));

        var png = OmrFormImage.Png(config);
        Assert.True(png.Length > 1000);
        Assert.Equal(0x89, png[0]);
    }

    [Fact]
    public void Khung_PlacesIdentityGridAboveTheQuestions()
    {
        var plan = OmrFormLayout.Measure(10, 4, OmrFormStyles.Khung);
        var headerBottom = plan.Header.Y + plan.Header.Height;

        Assert.Equal(60, plan.Codes[0].Bubbles.Count);
        Assert.Equal(30, plan.Codes[1].Bubbles.Count);
        Assert.True(plan.Bubbles.Min(item => item.Area.Y) > headerBottom);
        Assert.True(plan.Codes.SelectMany(grid => grid.Bubbles).Max(item => item.Area.Y + item.Area.Height) < headerBottom + 0.01);
        Assert.True(plan.Qr.Y + plan.Qr.Height < 1d / 3d);
        Assert.True(plan.Bubbles.Max(item => item.Area.Y + item.Area.Height) <= 1);

        var png = OmrFormImage.Png(OmrTemplateConfig.Grid(10, 4, OmrFormStyles.Khung));
        Assert.Equal(0x89, png[0]);

        var wide = OmrFormLayout.Measure(20, 4, OmrFormStyles.Khung);
        Assert.Equal(4, wide.Columns.Count);
        Assert.Equal(wide.Columns[0].Area.Y, wide.Columns[3].Area.Y, 3);
        var separation = wide.Columns[1].Area.X - (wide.Columns[0].Area.X + wide.Columns[0].Area.Width);
        Assert.True(separation >= 16d / OmrFormLayout.PageWidth);

        var wrapped = OmrFormLayout.Measure(25, 4, OmrFormStyles.Khung);
        Assert.Equal(5, wrapped.Columns.Count);
        Assert.Equal(wrapped.Columns[0].Area.X, wrapped.Columns[4].Area.X, 3);
        Assert.True(wrapped.Columns[4].Area.Y > wrapped.Columns[0].Area.Y);
    }

    [Fact]
    public void ClassicSheet_StacksThirtyQuestionsInEachOfFourColumns()
    {
        var plan = OmrFormLayout.Measure(120, 4, OmrFormStyles.PhieuThi);

        Assert.Equal(480, plan.Bubbles.Count);
        Assert.Equal(4, plan.Columns.Count);
        Assert.Equal(Enumerable.Range(1, 30), plan.Columns[0].Questions);
        Assert.Equal(Enumerable.Range(91, 30), plan.Columns[3].Questions);
        Assert.Equal(60, plan.Codes[0].Bubbles.Count);
        Assert.Equal(30, plan.Codes[1].Bubbles.Count);
        Assert.True(plan.Bubbles.Min(item => item.Area.Y) > plan.Header.Y + plan.Header.Height);
        Assert.True(plan.Qr.Y + plan.Qr.Height < 1d / 3d);
        Assert.True(plan.Qr.X + plan.Qr.Width < 0.5);
        Assert.True(plan.Bubbles.Max(item => item.Area.Y + item.Area.Height) <= 1);

        var first = plan.Bubbles.First(item => item.QuestionNumber == 1 && item.ChoiceIndex == 0);
        var nextColumn = plan.Bubbles.First(item => item.QuestionNumber == 31 && item.ChoiceIndex == 0);
        Assert.True(first.Area.X < nextColumn.Area.X);
        Assert.Equal(first.Area.Y, nextColumn.Area.Y, 3);

        var png = OmrFormImage.Png(OmrTemplateConfig.Grid(120, 4, OmrFormStyles.PhieuThi));
        Assert.Equal(0x89, png[0]);

        var shorter = OmrFormLayout.Measure(40, 4, OmrFormStyles.PhieuThi);
        Assert.Equal(160, shorter.Bubbles.Count);
        Assert.Equal(2, shorter.Columns.Count);
        Assert.Equal(40, shorter.Bubbles.Max(item => item.QuestionNumber));
    }

    [Fact]
    public void CheckSheet_MatchesTheA4QuizFormAndTheChosenCounts()
    {
        var parts = new List<(string Kind, int QuestionCount)> { ("BonDapAn", 32), ("DungSai", 2) };
        Assert.True(OmrTemplateConfig.TryFromCheck(parts, out var config));
        Assert.Equal(OmrFormStyles.KiemTra, config.Style);
        Assert.Equal(40, config.QuestionCount);
        Assert.Equal(144, config.Bubbles.Count);

        var plan = OmrFormLayout.For(config);
        Assert.Equal(4, plan.Columns.Count);
        Assert.Equal(Enumerable.Range(1, 10), plan.Columns[0].Questions);
        Assert.Equal(new[] { 31, 32 }, plan.Columns[3].Questions);
        Assert.Equal(60, plan.Codes[0].Bubbles.Count);
        Assert.Equal(30, plan.Codes[1].Bubbles.Count);
        Assert.True(plan.Bubbles.Min(item => item.Area.Y) > 1d / 3d);
        Assert.True(plan.Qr.Y + plan.Qr.Height < 1d / 3d);
        Assert.True(plan.Columns[0].Area.Width > 0.15);
        Assert.True(plan.Bubbles.Max(item => item.Area.Y + item.Area.Height) < 0.8);

        var png = OmrFormImage.Png(config);
        Assert.Equal(0x89, png[0]);
        Assert.True(png.Length > 1000);

        Assert.True(OmrTemplateConfig.TryFromCheck([("BonDapAn", 50), ("DungSai", 2)], out var wider));
        var wrapped = OmrFormLayout.For(wider);
        Assert.Equal(5, wrapped.Columns.Count);
        Assert.Equal(Enumerable.Range(41, 10), wrapped.Columns[4].Questions);
        Assert.Equal(wrapped.Columns[0].Area.Y, wrapped.Columns[3].Area.Y);
        Assert.True(wrapped.Columns[4].Area.Y > wrapped.Columns[0].Area.Y);
        Assert.True(wrapped.Truths[0].Area.Y > wrapped.Columns[4].Area.Y);
        Assert.True(wrapped.Bubbles.Max(item => item.Area.Y + item.Area.Height) < 1);
    }

    [Fact]
    public void EssaySheet_PlacesChoiceTruthAndDigitBubbles()
    {
        var parts = new List<(string Kind, int QuestionCount)> { ("BonDapAn", 8), ("DungSai", 4), ("DienSo", 2) };
        Assert.True(OmrTemplateConfig.TryFromEssay(parts, out var config));
        Assert.Equal(OmrFormStyles.TuLuan, config.Style);
        Assert.Equal(8 + 16 + 2, config.QuestionCount);
        Assert.Equal(8 * 4 + 16 * 2 + 2 * OmrSectionKinds.EssayDigitBubbles, config.Bubbles.Count);

        var plan = OmrFormLayout.For(config);
        Assert.Equal(8, plan.Codes[1].Columns);
        Assert.Equal(4, plan.Codes[2].Columns);
        Assert.Equal(4, plan.Truths.Count);
        Assert.Equal(2, plan.Digits.Count);
        Assert.True(plan.Bubbles.Min(item => item.Area.Y) > 1d / 3d);
        Assert.Contains(plan.Sections, item => item.Text == "PHẦN TỰ LUẬN");

        var png = OmrFormImage.Png(config);
        Assert.Equal(0x89, png[0]);
        Assert.True(png.Length > 1000);

        Assert.True(OmrTemplateConfig.TryFromEssay([("BonDapAn", 8), ("DungSai", 6), ("DienSo", 3)], out var fuller));
        var wide = OmrFormLayout.For(fuller);
        Assert.Equal(6, wide.Truths.Count);
        Assert.Equal(3, wide.Digits.Count);
        Assert.True(wide.Bubbles.Max(item => item.Area.X + item.Area.Width) < 1);
        Assert.True(wide.Truths.Max(item => item.Area.Y) > wide.Truths[0].Area.Y);
    }

    [Fact]
    public void ScanChoice_PrefersAClearQrSheetOverANoisyFrame()
    {
        var noisy = Read(qr: false, ambiguous: 8, clear: 2);
        var clean = Read(qr: true, ambiguous: 0, clear: 8);

        Assert.False(OmrScanChoice.Accepts(noisy));
        Assert.True(OmrScanChoice.Accepts(clean));
        Assert.True(OmrScanChoice.Score(clean) > OmrScanChoice.Score(noisy));
    }

    [Fact]
    public void DigitGrid_ReadsSignedDecimalAndRejectsTwoMarksInOneColumn()
    {
        var marks = new List<(int, string)>
        {
            (OmrDigitGrid.Index(0, 0), "Marked"),
            (OmrDigitGrid.Index(1, 3), "Marked"),
            (OmrDigitGrid.Index(2, 1), "Marked"),
            (OmrDigitGrid.Index(3, 7), "Marked")
        };

        Assert.True(OmrDigitGrid.TryRead(marks, out var text));
        Assert.Equal("-1,5", text);
        Assert.True(OmrDigitGrid.SameValue(" -1.5 ", text));

        marks.Add((OmrDigitGrid.Index(1, 4), "Marked"));
        Assert.False(OmrDigitGrid.TryRead(marks, out _));
    }

    [Fact]
    public void CodeGrid_ReadsARunOfDigitsAndRejectsAHole()
    {
        var columns = new List<OmrCodeGrid.Column>
        {
            new(0, 2, false),
            new(1, 6, false),
            new(2, 0, false),
            new(3, null, false)
        };
        Assert.True(OmrCodeGrid.TryRead(columns, out var text));
        Assert.Equal("260", text);

        columns[2] = new OmrCodeGrid.Column(2, null, false);
        columns[3] = new OmrCodeGrid.Column(3, 1, false);
        Assert.False(OmrCodeGrid.TryRead(columns, out _));
        Assert.True(OmrRules.SameExamCode("001", "DE1"));
        Assert.False(OmrRules.SameExamCode("10", "DE1"));
    }

    [Fact]
    public void Recognize_ReadsBubbledCodesWhenTheQrIsCovered()
    {
        Assert.True(OmrTemplateConfig.TryFromCheck([("BonDapAn", 10)], out var config));
        var plan = OmrFormLayout.For(config);
        var png = OmrFormImage.Png(plan, null, "26001", null, "101", null, fillExamCode: true);
        using var bitmap = SKBitmap.Decode(png);
        using var canvas = new SKCanvas(bitmap);
        var qr = OmrFormLayout.Pixels(plan.Qr);
        using var white = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(qr.Left - 6, qr.Top - 6, qr.Width + 12, qr.Height + 12), white);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        var read = new OmrRecognitionService().Recognize(data.ToArray(), config);

        Assert.Null(read.Error);
        Assert.False(read.QrFound);
        Assert.Equal("26001", read.StudentCode);
        Assert.Equal("101", read.ExamCode);
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

    [Fact]
    public void SheetPdf_PutsEachStudentOnAPage()
    {
        var config = OmrTemplateConfig.Grid(4, 4);
        var pdf = OmrFormImage.Pdf(config,
        [
            new OmrSheetCopy("Học sinh A", "HS001", "6A1", "DE1"),
            new OmrSheetCopy("Học sinh B", "HS002", "6A1", "DE1")
        ]);

        var text = Encoding.Latin1.GetString(pdf);
        Assert.StartsWith("%PDF", text);
        Assert.Equal(2, Regex.Matches(text, "/Type /Page[^s]").Count);
    }

    private static OmrRecognitionResult Read(bool qr, int ambiguous, int clear)
    {
        var bubbles = new List<OmrBubbleReading>();
        for (var index = 0; index < ambiguous; index++)
            bubbles.Add(new OmrBubbleReading { QuestionNumber = 1, ChoiceIndex = index, Status = nameof(OmrMarkStatus.Ambiguous) });
        for (var index = 0; index < clear; index++)
            bubbles.Add(new OmrBubbleReading { QuestionNumber = 1, ChoiceIndex = ambiguous + index, Status = nameof(OmrMarkStatus.Blank) });
        return new OmrRecognitionResult { QrFound = qr, ExamCode = qr ? "DE1" : null, Bubbles = bubbles };
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
