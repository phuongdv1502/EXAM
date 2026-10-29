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
        var plan = OmrFormLayout.For(config);
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
        if (plan.Style == OmrFormStyles.NhieuPhan)
        {
            DrawMinistrySheet(canvas, plan, studentName, studentCode, className, examCode);
            return;
        }

        if (plan.Style == OmrFormStyles.PhieuThi)
        {
            DrawClassicSheet(canvas, plan, studentName, studentCode, className, examCode);
            return;
        }

        if (plan.Codes.Count > 0)
        {
            DrawOfficialSheet(canvas, plan, studentName, studentCode, className, examCode);
            return;
        }

        var many = plan.Sections.Count > 0;
        using var ink = new SKPaint { Color = many ? new SKColor(136, 19, 55) : new SKColor(17, 24, 39), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var line = new SKPaint { Color = many ? new SKColor(190, 18, 60) : new SKColor(17, 24, 39), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = many ? 1.4f : 2 };
        using var thin = new SKPaint { Color = many ? new SKColor(253, 164, 175) : new SKColor(55, 65, 81), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var band = new SKPaint { Color = many ? new SKColor(255, 241, 242) : new SKColor(243, 244, 246), Style = SKPaintStyle.Fill };
        using var titleBand = new SKPaint { Color = many ? new SKColor(190, 18, 60) : new SKColor(243, 244, 246), Style = SKPaintStyle.Fill };
        using var titleInk = new SKPaint { Color = many ? SKColors.White : new SKColor(17, 24, 39), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var ring = new SKPaint { Color = many ? new SKColor(190, 18, 60) : new SKColor(17, 24, 39), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = many ? 1.6f : 2 };
        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), many ? 26 : 32);
        using var labelFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), many ? 14 : 18);
        using var bodyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), many ? 14 : 18);
        using var smallFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), many ? 12 : 15);

        DrawSheetHeader(canvas, plan, studentName, studentCode, className, examCode, ink, line, titleFont, labelFont, bodyFont, smallFont);

        foreach (var title in plan.Sections)
        {
            var bandBox = OmrFormLayout.Pixels(title.Area);
            canvas.DrawRoundRect(Rect(bandBox), 4, 4, titleBand);
            canvas.DrawText(title.Text, bandBox.Left + 12, bandBox.Top + 19, SKTextAlign.Left, labelFont, titleInk);
        }

        foreach (var column in plan.Columns)
        {
            var frame = OmrFormLayout.Pixels(column.Area);
            var head = SKRect.Create(frame.Left, frame.Top, frame.Width, column.HeaderHeight);
            canvas.DrawRect(head, band);
            canvas.DrawRect(Rect(frame), line);
            canvas.DrawLine(frame.Left, frame.Top + column.HeaderHeight, frame.Left + frame.Width, frame.Top + column.HeaderHeight, line);
            if (!many)
            {
                canvas.DrawLine(frame.Left + column.Gutter, frame.Top, frame.Left + column.Gutter, frame.Top + frame.Height, thin);
                var choiceWidth = (frame.Width - column.Gutter) / (double)column.ChoiceCount;
                for (var choice = 1; choice < column.ChoiceCount; choice++)
                {
                    var x = (float)(frame.Left + column.Gutter + choice * choiceWidth);
                    canvas.DrawLine(x, frame.Top, x, frame.Top + frame.Height, thin);
                }

                for (var row = 1; row < column.Questions.Count; row++)
                {
                    var y = frame.Top + column.HeaderHeight + row * column.RowHeight;
                    canvas.DrawLine(frame.Left, y, frame.Left + frame.Width, y, thin);
                }
            }

            var choiceWidthAll = (frame.Width - column.Gutter) / (double)column.ChoiceCount;
            var labelY = frame.Top + column.HeaderHeight - (many ? 8 : 12);
            canvas.DrawText("Câu", frame.Left + 8, labelY, SKTextAlign.Left, labelFont, ink);
            for (var choice = 0; choice < column.ChoiceCount; choice++)
            {
                var x = (float)(frame.Left + column.Gutter + (choice + 0.5) * choiceWidthAll);
                canvas.DrawText(ChoiceLabel(column.ChoiceKind, choice), x, labelY, SKTextAlign.Center, labelFont, ink);
            }

            for (var row = 0; row < column.Questions.Count; row++)
            {
                var question = column.Questions[row];
                if (question < 1)
                    continue;
                var y = frame.Top + column.HeaderHeight + row * column.RowHeight + column.RowHeight / 2f + 5;
                canvas.DrawText(question.ToString(), frame.Left + column.Gutter / 2f, y, SKTextAlign.Center, bodyFont, ink);
            }
        }

        DrawTruth(canvas, plan, ink, line, band, labelFont, bodyFont);
        DrawDigits(canvas, plan, ink, line, band, labelFont, smallFont);

        foreach (var slot in plan.Bubbles)
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            var pad = many ? 2 : 0;
            canvas.DrawOval(SKRect.Create(box.Left - pad, box.Top - pad, box.Width + pad * 2, box.Height + pad * 2), ring);
        }
    }

    // Phiếu in: tiêu đề đậm, chữ thường nhạt hơn, khung và vòng tròn nét mảnh.
    private static void DrawOfficialSheet(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode)
    {
        using var bold = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold);
        using var regular = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal);
        using var titleFont = new SKFont(bold, 30);
        using var sectionFont = new SKFont(bold, 16);
        using var strongFont = new SKFont(bold, 13);
        using var bodyFont = new SKFont(regular, 13);
        using var smallFont = new SKFont(regular, 12);
        using var ink = new SKPaint { Color = new SKColor(17, 17, 17), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var outer = new SKPaint { Color = new SKColor(45, 45, 45), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.05f };
        using var hair = new SKPaint { Color = new SKColor(196, 196, 196), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var ring = new SKPaint { Color = new SKColor(110, 110, 110), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.05f };
        using var square = new SKPaint { Color = new SKColor(12, 12, 12), Style = SKPaintStyle.Fill, IsAntialias = false };
        using var dots = new SKPaint
        {
            Color = new SKColor(150, 150, 150),
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash([1.2f, 2.8f], 0)
        };

        canvas.DrawText("PHIẾU TRẢ LỜI TRẮC NGHIỆM", OmrFormLayout.PageWidth / 2f, 48, SKTextAlign.Center, titleFont, ink);
        foreach (var field in plan.Fields)
            DrawDottedField(canvas, field, FieldValue(field.Label, studentName, studentCode, className), bodyFont, bodyFont, ink, dots);

        var header = OmrFormLayout.Pixels(plan.Header);
        canvas.DrawRect(Rect(header), outer);
        var info = plan.Fields.First(item => item.Label.StartsWith("1.", StringComparison.Ordinal));
        var split = OmrFormLayout.Pixels(info.Area).Left - 14;
        canvas.DrawLine(split, header.Top, split, header.Top + header.Height, outer);
        foreach (var grid in plan.Codes)
        {
            var edge = OmrFormLayout.Pixels(grid.Area).Left;
            canvas.DrawLine(edge, header.Top, edge, header.Top + header.Height, outer);
        }

        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRect(SKRect.Create(qr.Left - 6, qr.Top - 6, qr.Width + 12, qr.Height + 12), hair);
        canvas.DrawText("Mã phiếu", qr.Left + qr.Width / 2f, qr.Top + qr.Height + 22, SKTextAlign.Center, smallFont, ink);
        canvas.DrawRect(SKRect.Create(12, header.Top + 36, 12, 12), square);
        canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 24, header.Top + 36, 12, 12), square);

        foreach (var grid in plan.Codes)
            DrawCodeGrid(canvas, grid, strongFont, smallFont, ink, outer, hair, ring, square, studentCode, examCode);

        foreach (var title in plan.Sections)
        {
            var band = OmrFormLayout.Pixels(title.Area);
            canvas.DrawText(title.Text, band.Left, band.Top + 14, SKTextAlign.Left, sectionFont, ink);
        }

        DrawOfficialColumns(canvas, plan, strongFont, smallFont, ink, outer, hair, square);
        DrawOfficialTruth(canvas, plan, strongFont, smallFont, ink, outer, hair, square);
        DrawOfficialDigits(canvas, plan, strongFont, smallFont, ink, outer, hair, square);

        foreach (var slot in plan.Bubbles)
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            canvas.DrawOval(SKRect.Create(box.Left - 1, box.Top - 1, box.Width + 2, box.Height + 2), ring);
        }
    }

    // Trang trắc nghiệm mẫu Bộ: mực hồng, ba phần, mốc đen hai bên. Không vẽ trang tự luận.
    private static void DrawMinistrySheet(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode)
    {
        using var bold = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold);
        using var regular = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal);
        using var italic = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Italic);
        using var titleFont = new SKFont(bold, 26);
        using var strongFont = new SKFont(bold, 13);
        using var bodyFont = new SKFont(regular, 13);
        using var smallFont = new SKFont(regular, 12);
        using var noteFont = new SKFont(italic, 13);
        using var ink = new SKPaint { Color = new SKColor(32, 30, 31), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var rose = new SKPaint { Color = new SKColor(214, 32, 82), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.15f };
        using var hair = new SKPaint { Color = new SKColor(244, 168, 186), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var ring = new SKPaint { Color = new SKColor(214, 32, 82), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.15f };
        using var band = new SKPaint { Color = new SKColor(253, 232, 234), Style = SKPaintStyle.Fill };
        using var square = new SKPaint { Color = new SKColor(12, 12, 12), Style = SKPaintStyle.Fill, IsAntialias = false };
        using var dots = new SKPaint
        {
            Color = new SKColor(180, 120, 136),
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash([1.2f, 2.8f], 0)
        };

        canvas.DrawText("PHIẾU TRẢ LỜI TRẮC NGHIỆM", OmrFormLayout.PageWidth / 2f, 40, SKTextAlign.Center, titleFont, ink);
        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRect(SKRect.Create(qr.Left - 3, qr.Top - 3, qr.Width + 6, qr.Height + 6), hair);
        canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 22, 18, 10, 10), square);

        var proctors = plan.Fields.Where(item => item.Label.StartsWith("Giám thị", StringComparison.Ordinal)).ToList();
        var info = plan.Fields.Where(item => !item.Label.StartsWith("Giám thị", StringComparison.Ordinal)).ToList();
        if (proctors.Count == 2)
        {
            var topBox = OmrFormLayout.Pixels(proctors[0].Area);
            var bottomBox = OmrFormLayout.Pixels(proctors[1].Area);
            var right = info.Where(item => item.Label.Length > 0 && char.IsDigit(item.Label[0]))
                .Select(item =>
                {
                    var box = OmrFormLayout.Pixels(item.Area);
                    return box.Left + box.Width;
                })
                .DefaultIfEmpty(topBox.Left + topBox.Width)
                .Max() + 8;
            var bottom = bottomBox.Top + bottomBox.Height;
            canvas.DrawRect(SKRect.Create(topBox.Left, topBox.Top, right - topBox.Left, bottom - topBox.Top), rose);
            canvas.DrawLine(topBox.Left + topBox.Width, topBox.Top, topBox.Left + topBox.Width, bottom, rose);
            canvas.DrawLine(topBox.Left, topBox.Top + topBox.Height, topBox.Left + topBox.Width, topBox.Top + topBox.Height, rose);
            DrawProctor(canvas, topBox, "của Giám thị 1", smallFont, ink);
            DrawProctor(canvas, bottomBox, "của Giám thị 2", smallFont, ink);
        }

        foreach (var field in info)
        {
            var suffix = field.Label.StartsWith("Ngày thi", StringComparison.Ordinal) ? "/ 20"
                : field.Label.StartsWith("5.", StringComparison.Ordinal) ? "(Nam/Nữ)"
                : null;
            DrawDottedField(canvas, field, FieldValue(field.Label, studentName, studentCode, className), bodyFont, bodyFont, ink, dots, suffix, smallFont);
        }

        foreach (var grid in plan.Codes)
            DrawCodeGrid(canvas, grid, strongFont, smallFont, ink, rose, hair, ring, square, studentCode, examCode);

        var header = OmrFormLayout.Pixels(plan.Header);
        canvas.DrawText("Chú ý: Thí sinh cần đọc kỹ hướng dẫn ở mặt sau Phiếu này.", OmrFormLayout.PageWidth / 2f, header.Top + header.Height + 18, SKTextAlign.Center, noteFont, ink);

        DrawMinistryBands(canvas, plan, strongFont, ink, rose, band, square);
        DrawOfficialColumns(canvas, plan, strongFont, smallFont, ink, rose, hair, square);
        DrawOfficialTruth(canvas, plan, strongFont, smallFont, ink, rose, hair, square);
        DrawOfficialDigits(canvas, plan, strongFont, smallFont, ink, rose, hair, square);

        foreach (var slot in plan.Bubbles)
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            canvas.DrawOval(SKRect.Create(box.Left - 1, box.Top - 1, box.Width + 2, box.Height + 2), ring);
        }

        canvas.DrawRect(SKRect.Create(12, OmrFormLayout.PageHeight - 26, 10, 10), square);
        canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 22, OmrFormLayout.PageHeight - 26, 10, 10), square);
    }

    // Nền hồng và mốc đen canh theo từng phần của trang trắc nghiệm.
    private static void DrawMinistryBands(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKFont font,
        SKPaint ink,
        SKPaint rose,
        SKPaint band,
        SKPaint square)
    {
        var blocks = new List<(int Top, int Bottom)>();
        foreach (var column in plan.Columns)
        {
            var box = OmrFormLayout.Pixels(column.Area);
            blocks.Add((box.Top, box.Top + box.Height));
        }

        foreach (var truth in plan.Truths)
        {
            var box = OmrFormLayout.Pixels(truth.Area);
            blocks.Add((box.Top, box.Top + box.Height));
        }

        foreach (var digit in plan.Digits)
        {
            var box = OmrFormLayout.Pixels(digit.Area);
            blocks.Add((box.Top, box.Top + box.Height));
        }

        for (var index = 0; index < plan.Sections.Count; index++)
        {
            var title = OmrFormLayout.Pixels(plan.Sections[index].Area);
            var nextTop = index + 1 < plan.Sections.Count
                ? OmrFormLayout.Pixels(plan.Sections[index + 1].Area).Top
                : OmrFormLayout.PageHeight;
            var content = blocks.Where(item => item.Top >= title.Top && item.Top < nextTop).ToList();
            var bottom = content.Count == 0 ? title.Top + title.Height : content.Max(item => item.Bottom);
            canvas.DrawRect(SKRect.Create(28, title.Top - 4, OmrFormLayout.PageWidth - 56, bottom - title.Top + 12), band);
            canvas.DrawRect(SKRect.Create(12, title.Top, 10, 10), square);
            canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 22, title.Top, 10, 10), square);
            var chip = SKRect.Create(title.Left, title.Top - 2, 92, title.Height);
            canvas.DrawRect(chip, rose);
            canvas.DrawText(plan.Sections[index].Text, chip.Left + 8, chip.Top + 14, SKTextAlign.Left, font, ink);
        }
    }

    // Phiếu thi in sẵn: tiêu đề đậm, cột câu không khung, mốc đen hai bên, vòng tròn mảnh.
    private static void DrawClassicSheet(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode)
    {
        using var bold = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold);
        using var regular = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal);
        using var italic = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Italic);
        using var titleFont = new SKFont(bold, 28);
        using var strongFont = new SKFont(bold, 12);
        using var bodyFont = new SKFont(regular, 13);
        using var smallFont = new SKFont(regular, 12);
        using var noteFont = new SKFont(italic, 13);
        using var ink = new SKPaint { Color = new SKColor(17, 17, 17), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var outer = new SKPaint { Color = new SKColor(45, 45, 45), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.05f };
        using var hair = new SKPaint { Color = new SKColor(196, 196, 196), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var ring = new SKPaint { Color = new SKColor(110, 110, 110), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.05f };
        using var square = new SKPaint { Color = new SKColor(12, 12, 12), Style = SKPaintStyle.Fill, IsAntialias = false };
        using var dots = new SKPaint
        {
            Color = new SKColor(150, 150, 150),
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash([1.2f, 2.8f], 0)
        };

        canvas.DrawText("PHIẾU TRẢ LỜI TRẮC NGHIỆM", OmrFormLayout.PageWidth / 2f, 42, SKTextAlign.Center, titleFont, ink);
        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRect(SKRect.Create(qr.Left - 4, qr.Top - 4, qr.Width + 8, qr.Height + 8), hair);
        canvas.DrawText("(Phiếu mẫu)", qr.Left + qr.Width + 10, qr.Top + 16, SKTextAlign.Left, smallFont, ink);
        canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 24, 16, 11, 11), square);

        var proctors = plan.Fields.Where(item => item.Label.StartsWith("Cán bộ", StringComparison.Ordinal)).ToList();
        var info = plan.Fields.Where(item => !item.Label.StartsWith("Cán bộ", StringComparison.Ordinal)).ToList();
        if (proctors.Count == 2 && info.Count > 0)
        {
            var topBox = OmrFormLayout.Pixels(proctors[0].Area);
            var bottomBox = OmrFormLayout.Pixels(proctors[1].Area);
            var right = info.Where(item => char.IsDigit(item.Label[0]))
                .Select(item =>
                {
                    var box = OmrFormLayout.Pixels(item.Area);
                    return box.Left + box.Width;
                })
                .DefaultIfEmpty(topBox.Left + topBox.Width)
                .Max() + 8;
            var bottom = bottomBox.Top + bottomBox.Height;
            canvas.DrawRect(SKRect.Create(topBox.Left, topBox.Top, right - topBox.Left, bottom - topBox.Top), outer);
            canvas.DrawLine(topBox.Left + topBox.Width, topBox.Top, topBox.Left + topBox.Width, bottom, outer);
            canvas.DrawLine(topBox.Left, topBox.Top + topBox.Height, topBox.Left + topBox.Width, topBox.Top + topBox.Height, outer);
            DrawProctor(canvas, topBox, "của cán bộ coi thi 1", smallFont, ink);
            DrawProctor(canvas, bottomBox, "của cán bộ coi thi 2", smallFont, ink);
        }

        foreach (var field in info)
        {
            var suffix = field.Label.StartsWith("Ngày thi", StringComparison.Ordinal) ? "/ 20"
                : field.Label.StartsWith("5.", StringComparison.Ordinal) ? "(Nam/Nữ)"
                : null;
            DrawDottedField(
                canvas,
                field,
                FieldValue(field.Label, studentName, studentCode, className),
                bodyFont,
                bodyFont,
                ink,
                dots,
                suffix,
                smallFont);
        }

        foreach (var grid in plan.Codes)
            DrawCodeGrid(canvas, grid, strongFont, smallFont, ink, outer, hair, ring, square, studentCode, examCode);

        foreach (var title in plan.Sections)
        {
            var band = OmrFormLayout.Pixels(title.Area);
            canvas.DrawText(title.Text, OmrFormLayout.PageWidth / 2f, band.Top + 14, SKTextAlign.Center, noteFont, ink);
        }

        foreach (var slot in plan.Bubbles.Where(item => item.ChoiceIndex == 0))
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            canvas.DrawText(slot.QuestionNumber.ToString(), box.Left - 8, box.Top + box.Height - 1, SKTextAlign.Right, smallFont, ink);
        }

        foreach (var slot in plan.Bubbles)
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            canvas.DrawOval(SKRect.Create(box.Left - 1, box.Top - 1, box.Width + 2, box.Height + 2), ring);
        }

        // Mốc đen canh theo nhóm 5 câu, máy scan dùng để nhận mép phiếu.
        if (plan.Columns.Count > 0)
        {
            var first = plan.Columns[0];
            for (var index = 0; index < first.Questions.Count; index += 5)
            {
                var number = first.Questions[index];
                var bubble = plan.Bubbles.First(item => item.QuestionNumber == number && item.ChoiceIndex == 0);
                var box = OmrFormLayout.Pixels(bubble.Area);
                var y = box.Top + box.Height / 2f - 5;
                canvas.DrawRect(SKRect.Create(12, y, 10, 10), square);
                canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 24, y, 10, 10), square);
            }
        }

        canvas.DrawRect(SKRect.Create(12, OmrFormLayout.PageHeight - 28, 11, 11), square);
        canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 24, OmrFormLayout.PageHeight - 28, 11, 11), square);
    }

    private static void DrawProctor(SKCanvas canvas, (int Left, int Top, int Width, int Height) box, string line, SKFont font, SKPaint ink)
    {
        var center = box.Left + box.Width / 2f;
        var middle = box.Top + box.Height / 2f;
        canvas.DrawText("Họ tên, chữ ký", center, middle - 6, SKTextAlign.Center, font, ink);
        canvas.DrawText(line, center, middle + 12, SKTextAlign.Center, font, ink);
    }

    private static void DrawCodeGrid(
        SKCanvas canvas,
        OmrFormLayout.CodeGrid grid,
        SKFont strongFont,
        SKFont smallFont,
        SKPaint ink,
        SKPaint outer,
        SKPaint hair,
        SKPaint ring,
        SKPaint square,
        string? studentCode,
        string? examCode)
    {
        var area = OmrFormLayout.Pixels(grid.Area);
        canvas.DrawText(grid.Title, area.Left + 4, area.Top - 8, SKTextAlign.Left, strongFont, ink);
        canvas.DrawRect(Rect(area), outer);
        canvas.DrawLine(area.Left + grid.LabelWidth, area.Top, area.Left + grid.LabelWidth, area.Top + area.Height, hair);
        for (var column = 1; column < grid.Columns; column++)
        {
            var x = area.Left + grid.LabelWidth + column * grid.Cell;
            canvas.DrawLine(x, area.Top, x, area.Top + area.Height, hair);
        }

        canvas.DrawLine(area.Left, area.Top + grid.HeaderHeight, area.Left + area.Width, area.Top + grid.HeaderHeight, hair);
        for (var row = 1; row < 10; row++)
        {
            var y = area.Top + grid.HeaderHeight + row * grid.Cell;
            canvas.DrawLine(area.Left, y, area.Left + area.Width, y, hair);
        }

        var mark = 9;
        canvas.DrawRect(SKRect.Create(area.Left + (grid.LabelWidth - mark) / 2f, area.Top + (grid.HeaderHeight - mark) / 2f, mark, mark), square);
        for (var digit = 0; digit < 10; digit++)
        {
            var sample = grid.Bubbles.First(item => item.Column == 0 && item.Digit == digit);
            var bubble = OmrFormLayout.Pixels(sample.Area);
            canvas.DrawText(digit.ToString(), area.Left + grid.LabelWidth / 2f, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Center, smallFont, ink);
        }

        foreach (var bubble in grid.Bubbles)
        {
            var box = OmrFormLayout.Pixels(bubble.Area);
            canvas.DrawOval(SKRect.Create(box.Left - 1, box.Top - 1, box.Width + 2, box.Height + 2), ring);
        }

        var source = grid.Title.Contains("báo danh", StringComparison.OrdinalIgnoreCase) ? studentCode : examCode;
        if (!TryDigits(source, grid.Columns, out var digits))
            return;
        using var fill = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill, IsAntialias = false };
        for (var column = 0; column < digits.Length; column++)
        {
            var chosen = grid.Bubbles.First(item => item.Column == column && item.Digit == digits[column]);
            var box = OmrFormLayout.Pixels(chosen.Area);
            canvas.DrawOval(Rect(box), fill);
        }
    }

    private static void DrawOfficialColumns(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKFont strongFont,
        SKFont smallFont,
        SKPaint ink,
        SKPaint outer,
        SKPaint hair,
        SKPaint square)
    {
        foreach (var column in plan.Columns)
        {
            var frame = OmrFormLayout.Pixels(column.Area);
            canvas.DrawRect(Rect(frame), outer);
            canvas.DrawLine(frame.Left, frame.Top + column.HeaderHeight, frame.Left + frame.Width, frame.Top + column.HeaderHeight, hair);
            canvas.DrawRect(SKRect.Create(frame.Left + 5, frame.Top + (column.HeaderHeight - 9) / 2f, 9, 9), square);
            var first = column.Questions.FirstOrDefault(item => item > 0);
            if (first > 0)
            {
                foreach (var slot in plan.Bubbles.Where(item => item.QuestionNumber == first).OrderBy(item => item.ChoiceIndex))
                {
                    var bubble = OmrFormLayout.Pixels(slot.Area);
                    canvas.DrawText(ChoiceLabel(column.ChoiceKind, slot.ChoiceIndex), bubble.Left + bubble.Width / 2f, frame.Top + column.HeaderHeight - 6, SKTextAlign.Center, strongFont, ink);
                }
            }

            foreach (var question in column.Questions)
            {
                if (question < 1)
                    continue;
                var slot = plan.Bubbles.First(item => item.QuestionNumber == question && item.ChoiceIndex == 0);
                var bubble = OmrFormLayout.Pixels(slot.Area);
                canvas.DrawText(question.ToString(), bubble.Left - 7, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
            }
        }
    }

    private static void DrawOfficialTruth(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKFont strongFont,
        SKFont smallFont,
        SKPaint ink,
        SKPaint outer,
        SKPaint hair,
        SKPaint square)
    {
        foreach (var block in plan.Truths)
        {
            var frame = OmrFormLayout.Pixels(block.Area);
            canvas.DrawRect(Rect(frame), outer);
            canvas.DrawRect(SKRect.Create(frame.Left + 6, frame.Top + 6, 9, 9), square);
            canvas.DrawText(block.Title, frame.Left + frame.Width / 2f, frame.Top + 16, SKTextAlign.Center, strongFont, ink);
            canvas.DrawLine(frame.Left, frame.Top + block.HeaderHeight, frame.Left + frame.Width, frame.Top + block.HeaderHeight, hair);
            var first = plan.Bubbles.Where(slot => slot.QuestionNumber == block.Rows[0].QuestionNumber).OrderBy(slot => slot.ChoiceIndex).ToList();
            if (first.Count == 2)
            {
                var dung = OmrFormLayout.Pixels(first[0].Area);
                var sai = OmrFormLayout.Pixels(first[1].Area);
                canvas.DrawText("Đúng", dung.Left + dung.Width / 2f, frame.Top + block.HeaderHeight - 8, SKTextAlign.Center, smallFont, ink);
                canvas.DrawText("Sai", sai.Left + sai.Width / 2f, frame.Top + block.HeaderHeight - 8, SKTextAlign.Center, smallFont, ink);
            }

            for (var index = 0; index < block.Rows.Count; index++)
            {
                var row = plan.Bubbles.First(slot => slot.QuestionNumber == block.Rows[index].QuestionNumber && slot.ChoiceIndex == 0);
                var bubble = OmrFormLayout.Pixels(row.Area);
                canvas.DrawText(block.Rows[index].Label + ")", bubble.Left - 8, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
            }
        }
    }

    private static void DrawOfficialDigits(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKFont strongFont,
        SKFont smallFont,
        SKPaint ink,
        SKPaint outer,
        SKPaint hair,
        SKPaint square)
    {
        foreach (var block in plan.Digits)
        {
            var frame = OmrFormLayout.Pixels(block.Area);
            canvas.DrawRect(Rect(frame), outer);
            canvas.DrawRect(SKRect.Create(frame.Left + 5, frame.Top + (block.HeaderHeight - 9) / 2f, 9, 9), square);
            canvas.DrawText(block.Title, frame.Left + frame.Width / 2f, frame.Top + 18, SKTextAlign.Center, strongFont, ink);
            canvas.DrawLine(frame.Left, frame.Top + block.HeaderHeight, frame.Left + frame.Width, frame.Top + block.HeaderHeight, hair);
            var origin = plan.Bubbles.First(slot => slot.QuestionNumber == block.QuestionNumber && slot.ChoiceIndex == OmrDigitGrid.Index(0, 0));
            var box = OmrFormLayout.Pixels(origin.Area);
            var gridLeft = box.Left - (block.Cell - box.Width) / 2f;
            var gridTop = box.Top - (block.Cell - box.Height) / 2f;
            for (var column = 0; column <= OmrDigitGrid.Columns; column++)
            {
                var x = gridLeft + column * block.Cell;
                canvas.DrawLine(x, gridTop, x, gridTop + OmrDigitGrid.Rows * block.Cell, hair);
            }

            for (var row = 0; row <= OmrDigitGrid.Rows; row++)
            {
                var y = gridTop + row * block.Cell;
                canvas.DrawLine(gridLeft, y, gridLeft + OmrDigitGrid.Columns * block.Cell, y, hair);
            }

            for (var row = 0; row < OmrDigitGrid.Rows; row++)
            {
                var sample = plan.Bubbles.First(slot => slot.QuestionNumber == block.QuestionNumber && slot.ChoiceIndex == OmrDigitGrid.Index(0, row));
                var mark = OmrFormLayout.Pixels(sample.Area);
                canvas.DrawText(OmrDigitGrid.Symbol(row), gridLeft - 5, mark.Top + mark.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
            }
        }
    }

    private static void DrawDottedField(
        SKCanvas canvas,
        OmrFormLayout.LineField field,
        string? value,
        SKFont labelFont,
        SKFont bodyFont,
        SKPaint ink,
        SKPaint dots,
        string? suffix = null,
        SKFont? suffixFont = null)
    {
        var box = OmrFormLayout.Pixels(field.Area);
        var baseline = box.Top + box.Height - 2;
        canvas.DrawText(field.Label, box.Left, baseline, SKTextAlign.Left, labelFont, ink);
        var textWidth = labelFont.MeasureText(field.Label);
        var lineLeft = box.Left + textWidth + 8;
        var suffixWidth = string.IsNullOrEmpty(suffix) ? 0 : (suffixFont ?? labelFont).MeasureText(suffix) + 10;
        var lineRight = box.Left + box.Width - suffixWidth;
        if (lineRight > lineLeft + 16)
            canvas.DrawLine(lineLeft, baseline + 3, lineRight, baseline + 3, dots);
        if (suffixWidth > 0)
            canvas.DrawText(suffix, box.Left + box.Width, baseline, SKTextAlign.Right, suffixFont ?? labelFont, ink);
        if (!string.IsNullOrWhiteSpace(value))
            canvas.DrawText(value, lineLeft + 6, baseline - 1, SKTextAlign.Left, bodyFont, ink);
    }

    private static string? FieldValue(string label, string? studentName, string? studentCode, string? className)
    {
        if (label.Contains("Họ và tên", StringComparison.Ordinal))
            return studentName;
        if (label.Contains("Mã học sinh", StringComparison.Ordinal))
            return studentCode;
        if (label.Contains("Lớp", StringComparison.Ordinal))
            return className;
        return null;
    }

    // Chỉ tô khi cả mã là chữ số và vừa số cột, để ô trống trên mẫu trắng.
    private static bool TryDigits(string? value, int columns, out int[] digits)
    {
        digits = [];
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var text = value.Trim();
        if (text.Length < 1 || text.Length > columns || text.Any(item => !char.IsDigit(item)))
            return false;
        digits = text.Select(item => item - '0').ToArray();
        return true;
    }

    // Phần đầu phiếu: mã QR bên trái, thông tin thí sinh bên phải, không để một ô trống quá lớn.
    private static void DrawSheetHeader(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode,
        SKPaint ink,
        SKPaint line,
        SKFont titleFont,
        SKFont labelFont,
        SKFont bodyFont,
        SKFont smallFont)
    {
        var header = OmrFormLayout.Pixels(plan.Header);
        var compact = header.Height < 320;
        canvas.DrawRoundRect(Rect(header), compact ? 10 : 0, compact ? 10 : 0, line);
        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRoundRect(SKRect.Create(qr.Left - 8, qr.Top - 8, qr.Width + 16, qr.Height + 16), 6, 6, line);

        if (!compact)
        {
            var split = header.Left + header.Width / 2;
            canvas.DrawLine(split, header.Top, split, header.Top + header.Height, line);
            canvas.DrawText("PHIẾU TRẢ LỜI", split + 28, header.Top + 58, SKTextAlign.Left, titleFont, ink);
            canvas.DrawText(StyleLabel(plan.Style), split + 28, header.Top + 92, SKTextAlign.Left, bodyFont, ink);
            DrawField(canvas, split + 28, header.Top + 150, header.Left + header.Width - 28, "Họ và tên", studentName, labelFont, bodyFont, ink, line);
            DrawField(canvas, split + 28, header.Top + 214, header.Left + header.Width - 28, "Mã học sinh", studentCode, labelFont, bodyFont, ink, line);
            DrawField(canvas, split + 28, header.Top + 278, header.Left + header.Width - 28, "Lớp", className, labelFont, bodyFont, ink, line);
            DrawField(canvas, split + 28, header.Top + 342, header.Left + header.Width - 28, "Mã đề", examCode, labelFont, bodyFont, ink, line);
            canvas.DrawText("Tô kín một ô cho mỗi câu.", split + 28, header.Top + header.Height - 28, SKTextAlign.Left, smallFont, ink);
            return;
        }

        var textLeft = qr.Left + qr.Width + 28;
        var textRight = header.Left + header.Width - 24;
        canvas.DrawText("PHIẾU TRẢ LỜI TRẮC NGHIỆM", textLeft, header.Top + 36, SKTextAlign.Left, titleFont, ink);
        canvas.DrawText(StyleLabel(plan.Style), textLeft, header.Top + 58, SKTextAlign.Left, bodyFont, ink);
        var half = (textRight - textLeft - 24) / 2f;
        var row = header.Top + 102;
        DrawField(canvas, textLeft, row, textLeft + half, "Họ và tên", studentName, labelFont, bodyFont, ink, line);
        DrawField(canvas, textLeft + half + 24, row, textRight, "Lớp", className, labelFont, bodyFont, ink, line);
        DrawField(canvas, textLeft, row + 42, textLeft + half, "Mã học sinh", studentCode, labelFont, bodyFont, ink, line);
        DrawField(canvas, textLeft + half + 24, row + 42, textRight, "Mã đề", examCode, labelFont, bodyFont, ink, line);
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

    private static string StyleLabel(string style) => style switch
    {
        OmrFormStyles.MotCot => "Một cột",
        OmrFormStyles.Luoi => "Lưới ô",
        OmrFormStyles.NhieuPhan => "Nhiều phần",
        OmrFormStyles.PhieuThi => "Phiếu thi",
        _ => "Khung nhiều cột"
    };

    private static string ChoiceLabel(string kind, int index)
    {
        if (kind == OmrSectionKinds.DungSai)
            return index == 0 ? "Đ" : "S";
        return index < 26 ? ((char)('A' + index)).ToString() : (index + 1).ToString();
    }

    private static void DrawTruth(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKPaint ink,
        SKPaint line,
        SKPaint band,
        SKFont labelFont,
        SKFont bodyFont)
    {
        foreach (var block in plan.Truths)
        {
            var frame = OmrFormLayout.Pixels(block.Area);
            var title = SKRect.Create(frame.Left, frame.Top, frame.Width, 22);
            canvas.DrawRect(title, band);
            canvas.DrawRoundRect(Rect(frame), 6, 6, line);
            canvas.DrawText(block.Title, frame.Left + frame.Width / 2f, frame.Top + 16, SKTextAlign.Center, labelFont, ink);
            var first = plan.Bubbles.Where(slot => slot.QuestionNumber == block.Rows[0].QuestionNumber).OrderBy(slot => slot.ChoiceIndex).ToList();
            if (first.Count == 2)
            {
                var dung = OmrFormLayout.Pixels(first[0].Area);
                var sai = OmrFormLayout.Pixels(first[1].Area);
                canvas.DrawText("Đúng", dung.Left + dung.Width / 2f, frame.Top + 38, SKTextAlign.Center, labelFont, ink);
                canvas.DrawText("Sai", sai.Left + sai.Width / 2f, frame.Top + 38, SKTextAlign.Center, labelFont, ink);
            }

            for (var index = 0; index < block.Rows.Count; index++)
            {
                var y = frame.Top + block.HeaderHeight + index * block.RowHeight + block.RowHeight / 2f + 5;
                canvas.DrawText(block.Rows[index].Label + ")", frame.Left + block.LabelWidth + 16, y, SKTextAlign.Center, bodyFont, ink);
            }
        }
    }

    private static void DrawDigits(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKPaint ink,
        SKPaint line,
        SKPaint band,
        SKFont labelFont,
        SKFont smallFont)
    {
        foreach (var block in plan.Digits)
        {
            var frame = OmrFormLayout.Pixels(block.Area);
            var title = SKRect.Create(frame.Left, frame.Top, frame.Width, block.HeaderHeight);
            canvas.DrawRect(title, band);
            canvas.DrawRoundRect(Rect(frame), 6, 6, line);
            canvas.DrawText(block.Title, frame.Left + frame.Width / 2f, frame.Top + 19, SKTextAlign.Center, labelFont, ink);
            for (var row = 0; row < OmrDigitGrid.Rows; row++)
            {
                var sample = plan.Bubbles.First(slot => slot.QuestionNumber == block.QuestionNumber && slot.ChoiceIndex == OmrDigitGrid.Index(0, row));
                var box = OmrFormLayout.Pixels(sample.Area);
                canvas.DrawText(OmrDigitGrid.Symbol(row), box.Left - 14, box.Top + box.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
            }
        }
    }

    private static SKRect Rect((int Left, int Top, int Width, int Height) box) =>
        SKRect.Create(box.Left, box.Top, box.Width, box.Height);
}
