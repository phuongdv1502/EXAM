using AIQuizHSG.Application.Exams;
using AIQuizHSG.Domain.Entities;
using SkiaSharp;

namespace AIQuizHSG.Infrastructure.Exams;

// In đề theo một mã đảo: câu và phương án đúng thứ tự đã khóa trên biến thể.
public sealed class ExamPaperRenderer : IExamPaperRenderer
{
    public byte[] Pdf(Exam exam, ExamVariant variant)
    {
        const int width = 1240;
        const int height = 1754;
        var questionIds = ExamVariantLayout.QuestionIds(variant);
        var optionMap = ExamVariantLayout.OptionIds(variant);
        var byId = exam.Questions.ToDictionary(item => item.Id);

        using var stream = new MemoryStream();
        using var document = SKDocument.CreatePdf(stream);
        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold), 28);
        using var bodyFont = new SKFont(SKTypeface.FromFamilyName("Times New Roman"), 18);
        using var ink = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        var page = 0;
        SKCanvas? canvas = null;
        var y = 0f;
        void NewPage()
        {
            canvas = document.BeginPage(width, height);
            canvas.Clear(SKColors.White);
            y = 64;
            page++;
            canvas.DrawText($"{exam.Title} — Mã đề {variant.Code}", 64, y, SKTextAlign.Left, titleFont, ink);
            y += 36;
            canvas.DrawText($"Trang {page}", width - 64, 48, SKTextAlign.Right, bodyFont, ink);
        }

        NewPage();
        for (var index = 0; index < questionIds.Count; index++)
        {
            if (!byId.TryGetValue(questionIds[index], out var question))
                continue;
            var block = Estimate(question, optionMap.GetValueOrDefault(question.Id));
            if (y + block > height - 64)
            {
                document.EndPage();
                NewPage();
            }

            DrawQuestion(canvas!, question, index + 1, optionMap.GetValueOrDefault(question.Id), bodyFont, ink, ref y);
        }

        document.EndPage();
        document.Close();
        return stream.ToArray();
    }

    private static float Estimate(ExamQuestion question, IReadOnlyList<long>? optionIds)
    {
        var lines = Wrap(question.ContentSnapshot, 90).Count + 2;
        lines += optionIds?.Count * 2 ?? question.Options.Count * 2;
        return lines * 22f;
    }

    private static void DrawQuestion(
        SKCanvas canvas,
        ExamQuestion question,
        int number,
        IReadOnlyList<long>? optionIds,
        SKFont font,
        SKPaint ink,
        ref float y)
    {
        foreach (var line in Wrap($"Câu {number}. {question.ContentSnapshot}", 90))
        {
            canvas.DrawText(line, 64, y, SKTextAlign.Left, font, ink);
            y += 22;
        }

        y += 6;
        var options = optionIds is null
            ? question.Options.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).ToList()
            : optionIds.Select(id => question.Options.First(item => item.Id == id)).ToList();
        for (var index = 0; index < options.Count; index++)
        {
            var label = (char)('A' + index);
            foreach (var line in Wrap($"{label}. {options[index].ContentSnapshot}", 86))
            {
                canvas.DrawText(line, 84, y, SKTextAlign.Left, font, ink);
                y += 22;
            }
        }

        y += 16;
    }

    private static List<string> Wrap(string text, int width)
    {
        var words = (text ?? "").Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = "";
        foreach (var word in words)
        {
            var next = current.Length == 0 ? word : current + " " + word;
            if (next.Length > width && current.Length > 0)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = next;
            }
        }

        if (current.Length > 0)
            lines.Add(current);
        if (lines.Count == 0)
            lines.Add("");
        return lines;
    }
}
