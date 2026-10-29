using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Omr;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace AIQuizHSG.Infrastructure.Omr;

// Vẽ phiếu khung vuông đúng tọa độ mẫu, để bản in và máy chấm khớp nhau.
public static class OmrFormImage
{
    // Mẫu đúng khung mới thì vẽ phiếu đầy đủ. Mẫu tọa độ cũ thì vẽ đúng ô đã lưu.
    public static byte[] Png(OmrTemplateConfig config)
    {
        var plan = OmrFormLayout.Measure(config.QuestionCount, config.ChoiceCount);
        if (SameLayout(config, plan))
            return Png(plan, null, null, null, null, null);
        return Legacy(config);
    }

    public static byte[] Png(
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode,
        IReadOnlySet<(int Question, int Choice)>? marked)
    {
        using var image = new SKBitmap(OmrFormLayout.PageWidth, OmrFormLayout.PageHeight);
        using (var canvas = new SKCanvas(image))
        {
            canvas.Clear(SKColors.White);
            DrawChrome(canvas, plan, studentName, studentCode, className, examCode);
        }

        if (!string.IsNullOrWhiteSpace(studentCode) && !string.IsNullOrWhiteSpace(examCode))
            DrawQr(image, plan.Qr, OmrRules.QrPayload(studentCode, examCode));

        if (marked is not null)
        {
            using var canvas = new SKCanvas(image);
            using var fill = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill, IsAntialias = false };
            foreach (var slot in plan.Bubbles)
            {
                if (!marked.Contains((slot.QuestionNumber, slot.ChoiceIndex)))
                    continue;
                var box = OmrFormLayout.Pixels(slot.Area);
                canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), fill);
            }
        }

        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawChrome(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode)
    {
        using var ink = new SKPaint { Color = new SKColor(17, 24, 39), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var line = new SKPaint { Color = new SKColor(17, 24, 39), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
        using var thin = new SKPaint { Color = new SKColor(55, 65, 81), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var band = new SKPaint { Color = new SKColor(243, 244, 246), Style = SKPaintStyle.Fill };
        using var ring = new SKPaint { Color = new SKColor(17, 24, 39), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 32);
        using var labelFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 18);
        using var bodyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 18);
        using var smallFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 15);

        var header = OmrFormLayout.Pixels(plan.Header);
        canvas.DrawRect(Rect(header), line);
        var split = header.Left + header.Width / 2;
        canvas.DrawLine(split, header.Top, split, header.Top + header.Height, line);

        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRect(SKRect.Create(qr.Left - 14, qr.Top - 14, qr.Width + 28, qr.Height + 28), thin);
        canvas.DrawText("PHIẾU TRẢ LỜI", split + 28, header.Top + 58, SKTextAlign.Left, titleFont, ink);
        canvas.DrawText("Trắc nghiệm", split + 28, header.Top + 92, SKTextAlign.Left, bodyFont, ink);
        DrawField(canvas, split + 28, header.Top + 150, header.Left + header.Width - 28, "Họ và tên", studentName, labelFont, bodyFont, ink, thin);
        DrawField(canvas, split + 28, header.Top + 214, header.Left + header.Width - 28, "Mã học sinh", studentCode, labelFont, bodyFont, ink, thin);
        DrawField(canvas, split + 28, header.Top + 278, header.Left + header.Width - 28, "Lớp", className, labelFont, bodyFont, ink, thin);
        DrawField(canvas, split + 28, header.Top + 342, header.Left + header.Width - 28, "Mã đề", examCode, labelFont, bodyFont, ink, thin);
        canvas.DrawText("Tô kín một ô cho mỗi câu.", split + 28, header.Top + header.Height - 28, SKTextAlign.Left, smallFont, ink);

        foreach (var column in plan.Columns)
        {
            var frame = OmrFormLayout.Pixels(column.Area);
            var head = SKRect.Create(frame.Left, frame.Top, frame.Width, column.HeaderHeight);
            canvas.DrawRect(head, band);
            canvas.DrawRect(Rect(frame), line);
            canvas.DrawLine(frame.Left, frame.Top + column.HeaderHeight, frame.Left + frame.Width, frame.Top + column.HeaderHeight, line);
            canvas.DrawLine(frame.Left + column.Gutter, frame.Top, frame.Left + column.Gutter, frame.Top + frame.Height, thin);
            var choiceWidth = (frame.Width - column.Gutter) / (double)plan.ChoiceCount;
            for (var choice = 1; choice < plan.ChoiceCount; choice++)
            {
                var x = (float)(frame.Left + column.Gutter + choice * choiceWidth);
                canvas.DrawLine(x, frame.Top, x, frame.Top + frame.Height, thin);
            }

            for (var row = 1; row <= column.LastQuestion - column.FirstQuestion; row++)
            {
                var y = frame.Top + column.HeaderHeight + row * column.RowHeight;
                canvas.DrawLine(frame.Left, y, frame.Left + frame.Width, y, thin);
            }

            canvas.DrawText("Câu", frame.Left + 16, frame.Top + 32, SKTextAlign.Left, labelFont, ink);
            for (var choice = 0; choice < plan.ChoiceCount; choice++)
            {
                var x = (float)(frame.Left + column.Gutter + (choice + 0.5) * choiceWidth);
                canvas.DrawText(ChoiceLetter(choice), x, frame.Top + 32, SKTextAlign.Center, labelFont, ink);
            }

            for (var question = column.FirstQuestion; question <= column.LastQuestion; question++)
            {
                var row = question - column.FirstQuestion;
                var y = frame.Top + column.HeaderHeight + row * column.RowHeight + column.RowHeight / 2f + 6;
                canvas.DrawText(question.ToString(), frame.Left + column.Gutter / 2f, y, SKTextAlign.Center, bodyFont, ink);
            }
        }

        foreach (var slot in plan.Bubbles)
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), ring);
        }
    }

    private static void DrawField(
        SKCanvas canvas,
        float left,
        float baseline,
        float right,
        string label,
        string? value,
        SKFont labelFont,
        SKFont bodyFont,
        SKPaint ink,
        SKPaint line)
    {
        canvas.DrawText(label, left, baseline, SKTextAlign.Left, labelFont, ink);
        var valueLeft = left + 150;
        canvas.DrawLine(valueLeft, baseline + 8, right, baseline + 8, line);
        if (!string.IsNullOrWhiteSpace(value))
            canvas.DrawText(value, valueLeft + 8, baseline, SKTextAlign.Left, bodyFont, ink);
    }

    private static void DrawQr(SKBitmap image, OmrFormLayout.Box box, string payload)
    {
        var target = OmrFormLayout.Pixels(box);
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Width = target.Width, Height = target.Height, Margin = 1 }
        };
        var pixels = writer.Write(payload);
        for (var y = 0; y < pixels.Height && y < target.Height; y++)
        {
            for (var x = 0; x < pixels.Width && x < target.Width; x++)
            {
                var blue = pixels.Pixels[(y * pixels.Width + x) * 4];
                image.SetPixel(target.Left + x, target.Top + y, blue < 128 ? SKColors.Black : SKColors.White);
            }
        }
    }

    private static bool SameLayout(OmrTemplateConfig config, OmrFormLayout.Plan plan)
    {
        if (config.Bubbles.Count != plan.Bubbles.Count)
            return false;
        var stored = config.Bubbles.OrderBy(item => item.QuestionNumber).ThenBy(item => item.ChoiceIndex).ToList();
        var drawn = plan.Bubbles.OrderBy(item => item.QuestionNumber).ThenBy(item => item.ChoiceIndex).ToList();
        for (var index = 0; index < stored.Count; index++)
        {
            if (stored[index].QuestionNumber != drawn[index].QuestionNumber || stored[index].ChoiceIndex != drawn[index].ChoiceIndex)
                return false;
            if (Math.Abs(stored[index].X - drawn[index].Area.X) > 0.002
                || Math.Abs(stored[index].Y - drawn[index].Area.Y) > 0.002
                || Math.Abs(stored[index].Width - drawn[index].Area.Width) > 0.002
                || Math.Abs(stored[index].Height - drawn[index].Area.Height) > 0.002)
                return false;
        }

        return true;
    }

    private static byte[] Legacy(OmrTemplateConfig config)
    {
        using var image = new SKBitmap(OmrFormLayout.PageWidth, OmrFormLayout.PageHeight);
        using (var canvas = new SKCanvas(image))
        {
            canvas.Clear(SKColors.White);
            using var ink = new SKPaint { Color = new SKColor(17, 24, 39), IsAntialias = true };
            using var ring = new SKPaint { Color = new SKColor(17, 24, 39), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
            using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 16);
            var qr = OmrFormLayout.Pixels(new OmrFormLayout.Box(0.06, 0.05, 0.16, 0.12));
            canvas.DrawRect(SKRect.Create(qr.Left, qr.Top, qr.Width, qr.Height), ring);
            canvas.DrawText("Mã QR", qr.Left, qr.Top - 8, SKTextAlign.Left, font, ink);
            foreach (var bubble in config.Bubbles.OrderBy(item => item.QuestionNumber).ThenBy(item => item.ChoiceIndex))
            {
                var box = OmrFormLayout.Pixels(new OmrFormLayout.Box(bubble.X, bubble.Y, bubble.Width, bubble.Height));
                canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), ring);
                if (bubble.ChoiceIndex == 0)
                    canvas.DrawText(bubble.QuestionNumber.ToString(), box.Left - 28, box.Top + box.Height, SKTextAlign.Left, font, ink);
            }
        }

        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static string ChoiceLetter(int index) => index < 26 ? ((char)('A' + index)).ToString() : (index + 1).ToString();

    private static SKRect Rect((int Left, int Top, int Width, int Height) box) =>
        SKRect.Create(box.Left, box.Top, box.Width, box.Height);
}
