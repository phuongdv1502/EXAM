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

        if (plan.Style != OmrFormStyles.TuLuan)
            return Encode(image);

        return EssayBook(image);
    }

    private static byte[] Encode(SKBitmap image)
    {
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // Phiếu tự luận có thêm ba trang giấy: trang 2 có ô chéo, trang 3 và 4 là giấy kẻ.
    private static byte[] EssayBook(SKBitmap first)
    {
        const int gap = 28;
        var height = OmrFormLayout.PageHeight * 4 + gap * 3;
        using var book = new SKBitmap(OmrFormLayout.PageWidth, height);
        using var canvas = new SKCanvas(book);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(first, 0, 0);
        DrawEssayFollowPage(canvas, OmrFormLayout.PageHeight + gap);
        DrawEssayRuledPage(canvas, (OmrFormLayout.PageHeight + gap) * 2);
        DrawEssayRuledPage(canvas, (OmrFormLayout.PageHeight + gap) * 3);
        return Encode(book);
    }

    private static int EssayX(double value) => (int)Math.Round(value * OmrFormLayout.PageWidth / 550d);

    private static int EssayY(double value) => (int)Math.Round(value * OmrFormLayout.PageHeight / 770d);

    private static void DrawEssayFollowPage(SKCanvas canvas, int top)
    {
        using var border = EssayBorder();
        using var guide = EssayGuide();
        using var rule = EssayRule();
        using var paper = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(0, top, OmrFormLayout.PageWidth, OmrFormLayout.PageHeight), paper);
        var outer = EssayRect(27.5, 27.5, 495, 715, top);
        canvas.DrawRect(outer, border);
        var blank = EssayRect(41.2, 41.2, 467.5, 371.2, top);
        canvas.DrawRect(blank, border);
        canvas.DrawLine(blank.Left, blank.Top, blank.Right, blank.Bottom, guide);
        canvas.DrawLine(blank.Right, blank.Top, blank.Left, blank.Bottom, guide);
        var lines = EssayRect(41.2, 426.2, 391.9, 275, top);
        canvas.DrawRect(lines, rule);
        for (var y = 460.6; y <= 691.6; y += 19.3)
            canvas.DrawLine(lines.Left + 10, top + EssayY(y), lines.Right - 10, top + EssayY(y), rule);
        var side = EssayRect(446.9, 426.2, 61.9, 275, top);
        canvas.DrawRect(side, border);
        canvas.DrawLine(side.Left, side.Top, side.Right, side.Bottom, guide);
        canvas.DrawLine(side.Right, side.Top, side.Left, side.Bottom, guide);
    }

    private static void DrawEssayRuledPage(SKCanvas canvas, int top)
    {
        using var border = EssayBorder();
        using var rule = EssayRule();
        using var paper = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(0, top, OmrFormLayout.PageWidth, OmrFormLayout.PageHeight), paper);
        canvas.DrawRect(EssayRect(27.5, 27.5, 495, 715, top), border);
        var inner = EssayRect(41.2, 41.2, 467.5, 687.5, top);
        canvas.DrawRect(inner, rule);
        for (var y = 75.6; y <= 710.9; y += 19.3)
            canvas.DrawLine(inner.Left + 10, top + EssayY(y), inner.Right - 10, top + EssayY(y), rule);
    }

    private static SKRect EssayRect(double x, double y, double width, double height, int top) =>
        SKRect.Create(EssayX(x), top + EssayY(y), EssayX(width), EssayY(height));

    private static SKPaint EssayBorder() => new()
    {
        Color = new SKColor(20, 20, 20),
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f
    };

    private static SKPaint EssayGuide() => new()
    {
        Color = new SKColor(170, 170, 170),
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.1f
    };

    private static SKPaint EssayRule() => new()
    {
        Color = new SKColor(176, 176, 176),
        IsAntialias = false,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.1f,
        PathEffect = SKPathEffect.CreateDash([1.4f, 2.8f], 0)
    };

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

        if (plan.Style == OmrFormStyles.KiemTra)
        {
            DrawCheckSheet(canvas, plan, studentName, studentCode, className, examCode);
            return;
        }

        if (plan.Style == OmrFormStyles.TuLuan)
        {
            DrawEssaySheet(canvas, plan, studentName, studentCode, className, examCode);
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
        using var titleFont = new SKFont(bold, 34);
        using var strongFont = new SKFont(bold, 13);
        using var bodyFont = new SKFont(regular, 18);
        using var smallFont = new SKFont(regular, 12);
        using var noteFont = new SKFont(italic, 16);
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

        canvas.DrawText("PHIẾU TRẢ LỜI TRẮC NGHIỆM", OmrFormLayout.PageWidth / 2f, 68, SKTextAlign.Center, titleFont, ink);
        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRect(SKRect.Create(qr.Left - 2, qr.Top - 2, qr.Width + 4, qr.Height + 4), hair);
        canvas.DrawRect(SKRect.Create(OmrFormLayout.PageWidth - 22, 18, 10, 10), square);

        var proctors = plan.Fields.Where(item => item.Label.StartsWith("Giám thị", StringComparison.Ordinal)).ToList();
        var info = plan.Fields.Where(item => !item.Label.StartsWith("Giám thị", StringComparison.Ordinal)).ToList();
        // Hộp chữ ký đứng riêng, chữ nằm trên đầu mỗi nửa để còn chỗ ký.
        if (proctors.Count == 2)
        {
            var topBox = OmrFormLayout.Pixels(proctors[0].Area);
            var bottomBox = OmrFormLayout.Pixels(proctors[1].Area);
            var height = bottomBox.Top + bottomBox.Height - topBox.Top;
            canvas.DrawRect(SKRect.Create(topBox.Left, topBox.Top, topBox.Width, height), rose);
            canvas.DrawLine(topBox.Left, bottomBox.Top, topBox.Left + topBox.Width, bottomBox.Top, rose);
            DrawSignLabel(canvas, topBox, "của Giám thị 1", bodyFont, ink);
            DrawSignLabel(canvas, bottomBox, "của Giám thị 2", bodyFont, ink);
        }

        var numbered = info.Where(item => item.Label.Length > 0 && char.IsDigit(item.Label[0])).ToList();
        // Hộp thông tin cùng chiều cao với hộp chữ ký, cách một khe nhỏ.
        if (numbered.Count > 0 && proctors.Count == 2)
        {
            var line = OmrFormLayout.Pixels(numbered[0].Area);
            var topBox = OmrFormLayout.Pixels(proctors[0].Area);
            var bottomBox = OmrFormLayout.Pixels(proctors[1].Area);
            const int pad = 10;
            canvas.DrawRect(SKRect.Create(line.Left - pad, topBox.Top, line.Width + pad * 2, bottomBox.Top + bottomBox.Height - topBox.Top), rose);
        }

        foreach (var field in info)
        {
            // Ngày thi và ngày sinh kẻ thành ba đoạn, có gạch chéo như phiếu Bộ.
            if (field.Label.StartsWith("Ngày thi", StringComparison.Ordinal))
                DrawDateField(canvas, field, bodyFont, ink, dots, true, null);
            else if (field.Label.StartsWith("5.", StringComparison.Ordinal))
                DrawDateField(canvas, field, bodyFont, ink, dots, false, "(Nam/Nữ).");
            else
                DrawDottedField(canvas, field, FieldValue(field.Label, studentName, studentCode, className), bodyFont, bodyFont, ink, dots);
        }

        foreach (var grid in plan.Codes)
            DrawMinistryCode(canvas, grid, strongFont, smallFont, ink, rose, studentCode, examCode);

        if (proctors.Count == 2 && numbered.Count > 0)
        {
            var left = OmrFormLayout.Pixels(proctors[0].Area);
            var right = OmrFormLayout.Pixels(numbered[0].Area);
            var bottom = OmrFormLayout.Pixels(proctors[1].Area);
            var center = (left.Left + right.Left + right.Width + 10) / 2f;
            canvas.DrawText("Chú ý: Thí sinh cần đọc kỹ hướng dẫn ở mặt sau Phiếu này.", center, bottom.Top + bottom.Height + 28, SKTextAlign.Center, noteFont, ink);
        }

        // Đề mục Phần I, II, III lớn hơn chữ trong ô.
        using var sectionFont = new SKFont(bold, 20);
        DrawMinistryBands(canvas, plan, sectionFont, ink, rose, band, square);
        // Ô đáp án nền trắng, tách khỏi dải hồng phía sau. Số câu và A–D in đậm.
        using var paper = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = false };
        using var markFont = new SKFont(bold, 14);
        DrawOfficialColumns(canvas, plan, markFont, markFont, ink, rose, hair, square, paper);
        DrawMinistryTruth(canvas, plan, markFont, ink, rose, square, paper);
        DrawMinistryDigits(canvas, plan, markFont, ink, rose, hair, square, paper);

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
            var chip = SKRect.Create(title.Left, title.Top, 148, title.Height);
            canvas.DrawRect(chip, rose);
            canvas.DrawText(plan.Sections[index].Text, chip.Left + 12, chip.Top + title.Height - 8, SKTextAlign.Left, font, ink);
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

    // Phiếu kiểm tra A4: Times New Roman, khung chữ ký, số báo danh, phần I theo cột và phần II đúng/sai.
    // Phiếu có phần giấy kẻ: đầu phiếu, ba phần trắc nghiệm, rồi khung tự luận.
    private static void DrawEssaySheet(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode)
    {
        using var bold = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold);
        using var regular = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal);
        using var titleFont = new SKFont(bold, 28);
        using var sectionFont = new SKFont(bold, 16);
        using var strongFont = new SKFont(bold, 13);
        using var bodyFont = new SKFont(regular, 14);
        using var smallFont = new SKFont(bold, 12);
        using var ink = new SKPaint { Color = new SKColor(17, 17, 17), IsAntialias = true, Style = SKPaintStyle.Fill };
        using var outer = new SKPaint { Color = new SKColor(20, 20, 20), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
        using var hair = new SKPaint { Color = new SKColor(40, 40, 40), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var ring = new SKPaint { Color = new SKColor(20, 20, 20), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f };
        using var square = new SKPaint { Color = new SKColor(12, 12, 12), Style = SKPaintStyle.Fill, IsAntialias = false };
        using var dots = new SKPaint
        {
            Color = new SKColor(120, 120, 120),
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = SKPathEffect.CreateDash([1.2f, 2.6f], 0)
        };

        var frame = OmrFormLayout.Pixels(plan.Header);
        canvas.DrawRect(Rect(frame), outer);
        canvas.DrawText("PHIẾU TRẢ LỜI TRẮC NGHIỆM", frame.Left + frame.Width * 0.42f, frame.Top + 28, SKTextAlign.Center, titleFont, ink);
        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRect(SKRect.Create(qr.Left, qr.Top, qr.Width, qr.Height), hair);
        Mark(canvas, square, frame.Left, frame.Top);
        Mark(canvas, square, frame.Left + frame.Width - 12, frame.Top);
        Mark(canvas, square, frame.Left, frame.Top + frame.Height - 12);
        Mark(canvas, square, frame.Left + frame.Width - 12, frame.Top + frame.Height - 12);

        foreach (var field in plan.Fields.Where(item => item.Label.StartsWith("Cán bộ", StringComparison.Ordinal)))
        {
            var box = OmrFormLayout.Pixels(field.Area);
            canvas.DrawRect(Rect(box), outer);
            canvas.DrawText("Họ tên, chữ ký", box.Left + box.Width / 2f, box.Top + 22, SKTextAlign.Center, bodyFont, ink);
            canvas.DrawText(field.Label.EndsWith('1') ? "của cán bộ coi thi 1" : "của cán bộ coi thi 2", box.Left + box.Width / 2f, box.Top + 40, SKTextAlign.Center, bodyFont, ink);
        }

        var staff = plan.Fields.Where(item => item.Label.StartsWith("Cán bộ", StringComparison.Ordinal)).Select(item => OmrFormLayout.Pixels(item.Area)).ToList();
        var info = plan.Fields.Where(item => item.Label.StartsWith("1.", StringComparison.Ordinal) || item.Label.StartsWith("2.", StringComparison.Ordinal) || item.Label.StartsWith("3.", StringComparison.Ordinal)).ToList();
        if (staff.Count == 2 && info.Count > 0)
        {
            var first = OmrFormLayout.Pixels(info[0].Area);
            var last = OmrFormLayout.Pixels(info[^1].Area);
            var left = staff[0].Left;
            var width = staff[1].Left + staff[1].Width - left;
            canvas.DrawRect(SKRect.Create(left, first.Top - 12, width, last.Top + last.Height - first.Top + 18), outer);
        }

        foreach (var field in plan.Fields.Where(item => !item.Label.StartsWith("Cán bộ", StringComparison.Ordinal)))
            DrawDottedField(canvas, field, FieldValue(field.Label, studentName, studentCode, className), bodyFont, bodyFont, ink, dots);

        var term = plan.Fields.FirstOrDefault(item => item.Label.StartsWith("Kỳ thi", StringComparison.Ordinal));
        var termBox = OmrFormLayout.Pixels(term.Area);
        var writeMiddle = termBox.Top + termBox.Height - 2;
        foreach (var grid in plan.Codes)
            DrawEssayCode(canvas, grid, strongFont, smallFont, ink, outer, writeMiddle);

        foreach (var title in plan.Sections.Where(item => item.Text != "PHẦN TỰ LUẬN"))
        {
            var band = OmrFormLayout.Pixels(title.Area);
            canvas.DrawText(title.Text, band.Left, band.Top + 14, SKTextAlign.Left, sectionFont, ink);
        }

        foreach (var column in plan.Columns)
        {
            // Vòng A–D nhỏ hơn khoảng cách cột và cách hàng ô tô một khoảng.
            var letters = new[] { "A", "B", "C", "D" };
            for (var choice = 0; choice < letters.Length && column.Questions.Count > 0; choice++)
            {
                var slot = plan.Bubbles.First(item => item.QuestionNumber == column.Questions[0] && item.ChoiceIndex == choice);
                var bubble = OmrFormLayout.Pixels(slot.Area);
                var mark = bubble.Width + 2;
                var cx = bubble.Left + bubble.Width / 2f;
                var cy = bubble.Top - 8 - mark / 2f;
                canvas.DrawOval(SKRect.Create(cx - mark / 2f, cy - mark / 2f, mark, mark), ring);
                var text = letters[choice];
                strongFont.MeasureText(text, out var bounds);
                canvas.DrawText(text, cx, cy - bounds.MidY, SKTextAlign.Center, strongFont, ink);
            }

            foreach (var question in column.Questions)
            {
                var slot = plan.Bubbles.First(item => item.QuestionNumber == question && item.ChoiceIndex == 0);
                var bubble = OmrFormLayout.Pixels(slot.Area);
                canvas.DrawText((question - column.Questions[0] + 1).ToString(), bubble.Left - 8, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
            }
        }

        foreach (var block in plan.Truths)
        {
            var area = OmrFormLayout.Pixels(block.Area);
            canvas.DrawText(block.Title, area.Left + 8, area.Top + 12, SKTextAlign.Left, strongFont, ink);
            if (block.Rows.Count > 0)
            {
                var dung = plan.Bubbles.First(item => item.QuestionNumber == block.Rows[0].QuestionNumber && item.ChoiceIndex == 0);
                var sai = plan.Bubbles.First(item => item.QuestionNumber == block.Rows[0].QuestionNumber && item.ChoiceIndex == 1);
                var dungBox = OmrFormLayout.Pixels(dung.Area);
                var saiBox = OmrFormLayout.Pixels(sai.Area);
                canvas.DrawText("Đ", dungBox.Left + dungBox.Width / 2f, dungBox.Top - 14, SKTextAlign.Center, strongFont, ink);
                canvas.DrawText("S", saiBox.Left + saiBox.Width / 2f, saiBox.Top - 14, SKTextAlign.Center, strongFont, ink);
            }

            foreach (var row in block.Rows)
            {
                var slot = plan.Bubbles.First(item => item.QuestionNumber == row.QuestionNumber && item.ChoiceIndex == 0);
                var bubble = OmrFormLayout.Pixels(slot.Area);
                canvas.DrawText(row.Label, bubble.Left - 6, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
            }
        }

        foreach (var block in plan.Digits)
        {
            var area = OmrFormLayout.Pixels(block.Area);
            canvas.DrawText(block.Title, area.Left + block.LabelWidth, area.Top + 14, SKTextAlign.Left, strongFont, ink);
            var anchor = OmrFormLayout.Pixels(plan.Bubbles.First(item => item.QuestionNumber == block.QuestionNumber && item.ChoiceIndex == OmrDigitGrid.Index(0, 0)).Area);
            for (var row = 0; row < OmrDigitGrid.Rows; row++)
            {
                var slot = plan.Bubbles.First(item => item.QuestionNumber == block.QuestionNumber && item.ChoiceIndex % OmrDigitGrid.Rows == row);
                var bubble = OmrFormLayout.Pixels(slot.Area);
                canvas.DrawText(OmrDigitGrid.Symbol(row), anchor.Left - 6, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
            }
        }

        var essay = plan.Sections.FirstOrDefault(item => item.Text == "PHẦN TỰ LUẬN");
        if (essay.Text is not null)
        {
            var area = OmrFormLayout.Pixels(essay.Area);
            using var faint = new SKPaint
            {
                Color = new SKColor(176, 176, 176),
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.1f,
                PathEffect = SKPathEffect.CreateDash([1.4f, 2.8f], 0)
            };
            using var dash = new SKPaint
            {
                Color = new SKColor(176, 176, 176),
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.2f,
                PathEffect = SKPathEffect.CreateDash([8f, 5f], 0)
            };
            canvas.DrawRect(Rect(area), dash);
            canvas.DrawText(essay.Text, area.Left + area.Width / 2f, area.Top + 22, SKTextAlign.Center, sectionFont, ink);
            for (var line = 36; line < area.Height - 8; line += 22)
                canvas.DrawLine(area.Left + 12, area.Top + line, area.Left + area.Width - 12, area.Top + line, faint);
        }

        foreach (var slot in plan.Bubbles)
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), ring);
        }

        foreach (var grid in plan.Codes)
        {
            foreach (var bubble in grid.Bubbles)
            {
                var box = OmrFormLayout.Pixels(bubble.Area);
                canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), ring);
            }
        }
    }

    private static void DrawEssayCode(
        SKCanvas canvas,
        OmrFormLayout.CodeGrid grid,
        SKFont strongFont,
        SKFont smallFont,
        SKPaint ink,
        SKPaint outer,
        float writeMiddle)
    {
        var first = OmrFormLayout.Pixels(grid.Bubbles.First(item => item.Column == 0 && item.Digit == 0).Area);
        var last = OmrFormLayout.Pixels(grid.Bubbles.First(item => item.Column == grid.Columns - 1 && item.Digit == 0).Area);
        var pitch = grid.Columns < 2 ? first.Width : (last.Left - first.Left) / (float)(grid.Columns - 1);
        var cellH = Math.Max(first.Height + 14, 32);
        var origin = first.Left + first.Width / 2f - pitch / 2f;
        var comma = grid.Title.Contains("tự luận", StringComparison.OrdinalIgnoreCase);
        var commaGap = comma ? 8f : 0;
        canvas.DrawText(grid.Title, origin, writeMiddle, SKTextAlign.Left, strongFont, ink);
        var writeTop = writeMiddle + 8;
        if (comma)
        {
            // Dấu phảy hạ xuống chân ô điểm, đuôi chạm mép dưới như phiếu gốc.
            using var commaFont = new SKFont(strongFont.Typeface, 16);
            commaFont.MeasureText(",", out var bounds);
            canvas.DrawText(",", origin + pitch + commaGap / 2f, writeTop + cellH - bounds.Bottom, SKTextAlign.Center, commaFont, ink);
        }
        for (var column = 0; column < grid.Columns; column++)
        {
            var shift = comma && column > 0 ? commaGap : 0;
            canvas.DrawRect(SKRect.Create(origin + column * pitch + shift, writeTop, pitch, cellH), outer);
        }
        for (var digit = 0; digit < 10; digit++)
        {
            var bubble = OmrFormLayout.Pixels(grid.Bubbles.First(item => item.Column == 0 && item.Digit == digit).Area);
            canvas.DrawText(digit.ToString(), bubble.Left - 8, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Right, smallFont, ink);
        }
    }

    private static void DrawCheckSheet(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        string? studentName,
        string? studentCode,
        string? className,
        string? examCode)
    {
        using var bold = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Bold);
        using var regular = SKTypeface.FromFamilyName("Times New Roman", SKFontStyle.Normal);
        using var titleFont = new SKFont(bold, 34);
        using var sectionFont = new SKFont(bold, 22);
        using var strongFont = new SKFont(bold, 16);
        using var bodyFont = new SKFont(regular, 16);
        using var smallFont = new SKFont(regular, 14);
        using var smallBold = new SKFont(bold, 14);
        using var ink = new SKPaint { Color = new SKColor(17, 17, 17), IsAntialias = true, Style = SKPaintStyle.Fill };
        // Viền các bảng dùng nét mảnh.
        using var outer = new SKPaint { Color = new SKColor(20, 20, 20), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
        using var hair = new SKPaint { Color = new SKColor(40, 40, 40), IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        using var ring = new SKPaint { Color = new SKColor(20, 20, 20), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.3f };
        using var square = new SKPaint { Color = new SKColor(12, 12, 12), Style = SKPaintStyle.Fill, IsAntialias = false };
        using var dots = new SKPaint
        {
            Color = new SKColor(90, 90, 90),
            IsAntialias = false,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.1f,
            PathEffect = SKPathEffect.CreateDash([1.4f, 2.6f], 0)
        };

        canvas.DrawText("PHIẾU TRẢ LỜI TRẮC NGHIỆM", OmrFormLayout.PageWidth / 2f, 50, SKTextAlign.Center, titleFont, ink);
        var qr = OmrFormLayout.Pixels(plan.Qr);
        canvas.DrawRect(SKRect.Create(qr.Left, qr.Top, qr.Width, qr.Height), hair);
        Mark(canvas, square, OmrFormLayout.PageWidth - 36, 28);

        var header = OmrFormLayout.Pixels(plan.Header);
        canvas.DrawRect(Rect(header), outer);
        var staff = plan.Fields.Where(item => item.Label is "Cán bộ coi thi" or "Giám khảo" or "Điểm bài kiểm tra").ToList();
        if (staff.Count == 3)
        {
            var widths = staff.Select(item => OmrFormLayout.Pixels(item.Area)).ToList();
            var split = widths[0].Left + widths[0].Width;
            canvas.DrawLine(split, header.Top, split, header.Top + header.Height, outer);
            canvas.DrawLine(header.Left, widths[1].Top, split, widths[1].Top, outer);
            canvas.DrawLine(header.Left, widths[2].Top, split, widths[2].Top, outer);
            DrawCenteredLines(canvas, widths[0], ["Chữ ký, họ tên của", "Cán bộ coi thi"], bodyFont, strongFont, ink);
            DrawCenteredLines(canvas, widths[1], ["Chữ ký, họ tên của", "Giám khảo"], bodyFont, strongFont, ink);
            DrawScoreBox(canvas, widths[2], strongFont, bodyFont, ink, dots);
        }

        foreach (var field in plan.Fields.Where(item => item.Label is not ("Cán bộ coi thi" or "Giám khảo" or "Điểm bài kiểm tra")))
        {
            if (field.Label == "TRƯỜNG:")
            {
                DrawDottedField(canvas, field, null, strongFont, bodyFont, ink, dots);
                continue;
            }

            var suffix = field.Label.StartsWith("5.", StringComparison.Ordinal) ? "( Nam / Nữ )" : null;
            DrawDottedField(canvas, field, FieldValue(field.Label, studentName, studentCode, className), bodyFont, bodyFont, ink, dots, suffix, smallFont);
        }

        var rule = plan.Fields.FirstOrDefault(item => item.Label.StartsWith("4.", StringComparison.Ordinal));
        if (rule.Label is not null)
        {
            var box = OmrFormLayout.Pixels(rule.Area);
            canvas.DrawLine(box.Left, box.Top + box.Height + 28, box.Left + box.Width, box.Top + box.Height + 28, dots);
        }

        foreach (var grid in plan.Codes)
            DrawCheckCode(canvas, grid, strongFont, smallBold, ink, outer, ring, studentCode, examCode);

        foreach (var title in plan.Sections)
        {
            var band = OmrFormLayout.Pixels(title.Area);
            canvas.DrawText(title.Text, band.Left, band.Top + 18, SKTextAlign.Left, sectionFont, ink);
        }

        foreach (var column in plan.Columns)
        {
            var frame = OmrFormLayout.Pixels(column.Area);
            canvas.DrawRect(Rect(frame), outer);
            var first = column.Questions[0];
            for (var choice = 0; choice < 4; choice++)
            {
                var slot = plan.Bubbles.First(item => item.QuestionNumber == first && item.ChoiceIndex == choice);
                var bubble = OmrFormLayout.Pixels(slot.Area);
                canvas.DrawText(((char)('A' + choice)).ToString(), bubble.Left + bubble.Width / 2f, frame.Top + 24, SKTextAlign.Center, strongFont, ink);
            }

            // Số câu, chữ đáp án và số 0–9 cạnh ô tô in đậm.
            for (var row = 0; row < column.Questions.Count; row++)
            {
                var number = column.Questions[row];
                var y = frame.Top + column.HeaderHeight + row * column.RowHeight + column.RowHeight / 2f + 5;
                canvas.DrawText(number.ToString(), frame.Left + column.Gutter / 2f, y, SKTextAlign.Center, strongFont, ink);
            }

            Mark(canvas, square, frame.Left + frame.Width + 10, frame.Top + frame.Height / 2f - 5);
        }

        if (plan.Truths.Count == 1)
            DrawCheckTruth(canvas, plan.Truths[0], strongFont, bodyFont, ink, outer);

        foreach (var slot in plan.Bubbles)
        {
            var box = OmrFormLayout.Pixels(slot.Area);
            canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), ring);
        }

        Mark(canvas, square, 24, OmrFormLayout.PageHeight - 40);
        Mark(canvas, square, OmrFormLayout.PageWidth - 36, OmrFormLayout.PageHeight - 40);
    }

    private static void DrawCenteredLines(SKCanvas canvas, (int Left, int Top, int Width, int Height) box, string[] lines, SKFont body, SKFont strong, SKPaint ink)
    {
        var y = box.Top + box.Height / 2f - 8;
        canvas.DrawText(lines[0], box.Left + box.Width / 2f, y, SKTextAlign.Center, body, ink);
        canvas.DrawText(lines[1], box.Left + box.Width / 2f, y + 22, SKTextAlign.Center, strong, ink);
    }

    private static void DrawScoreBox(SKCanvas canvas, (int Left, int Top, int Width, int Height) box, SKFont strong, SKFont body, SKPaint ink, SKPaint dots)
    {
        canvas.DrawText("Điểm bài kiểm tra", box.Left + box.Width / 2f, box.Top + 28, SKTextAlign.Center, strong, ink);
        canvas.DrawText("Bằng số:", box.Left + 16, box.Top + 62, SKTextAlign.Left, body, ink);
        canvas.DrawLine(box.Left + 92, box.Top + 66, box.Left + box.Width - 16, box.Top + 66, dots);
        canvas.DrawText("Bằng chữ:", box.Left + 16, box.Top + 98, SKTextAlign.Left, body, ink);
        canvas.DrawLine(box.Left + 100, box.Top + 102, box.Left + box.Width - 16, box.Top + 102, dots);
    }

    private static void DrawCheckCode(
        SKCanvas canvas,
        OmrFormLayout.CodeGrid grid,
        SKFont strongFont,
        SKFont smallFont,
        SKPaint ink,
        SKPaint outer,
        SKPaint ring,
        string? studentCode,
        string? examCode)
    {
        var area = OmrFormLayout.Pixels(grid.Area);
        canvas.DrawText(grid.Title, area.Left, area.Top - 8, SKTextAlign.Left, strongFont, ink);
        canvas.DrawRect(Rect(area), outer);
        var cellW = area.Width / (float)grid.Columns;
        for (var column = 1; column < grid.Columns; column++)
            canvas.DrawLine(area.Left + column * cellW, area.Top, area.Left + column * cellW, area.Top + area.Height, outer);
        canvas.DrawLine(area.Left, area.Top + grid.HeaderHeight, area.Left + area.Width, area.Top + grid.HeaderHeight, outer);
        for (var row = 1; row < 10; row++)
        {
            var y = area.Top + grid.HeaderHeight + row * grid.Cell;
            canvas.DrawLine(area.Left, y, area.Left + area.Width, y, outer);
        }

        // Số 0–9 khoanh tròn ngoài lưới: số báo danh bên trái, mã đề bên phải.
        var onRight = grid.Title.Contains("Mã đề", StringComparison.Ordinal);
        const float mark = 20;
        for (var digit = 0; digit < 10; digit++)
        {
            var sample = grid.Bubbles.First(item => item.Column == 0 && item.Digit == digit);
            var bubble = OmrFormLayout.Pixels(sample.Area);
            var cy = bubble.Top + bubble.Height / 2f;
            var cx = onRight
                ? area.Left + area.Width + 8 + mark / 2f
                : area.Left - 8 - mark / 2f;
            canvas.DrawOval(SKRect.Create(cx - mark / 2f, cy - mark / 2f, mark, mark), ring);
            var text = digit.ToString();
            smallFont.MeasureText(text, out var bounds);
            canvas.DrawText(text, cx, cy - bounds.MidY, SKTextAlign.Center, smallFont, ink);
        }

        foreach (var bubble in grid.Bubbles)
        {
            var box = OmrFormLayout.Pixels(bubble.Area);
            canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), ring);
        }

        var source = grid.Title.Contains("báo danh", StringComparison.OrdinalIgnoreCase) ? studentCode : examCode;
        if (!TryDigits(source, grid.Columns, out var digits))
            return;
        using var fill = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill, IsAntialias = false };
        for (var column = 0; column < digits.Length; column++)
        {
            var chosen = grid.Bubbles.First(item => item.Column == column && item.Digit == digits[column]);
            canvas.DrawOval(Rect(OmrFormLayout.Pixels(chosen.Area)), fill);
        }
    }

    private static void DrawCheckTruth(
        SKCanvas canvas,
        OmrFormLayout.TruthBlock block,
        SKFont strongFont,
        SKFont bodyFont,
        SKPaint ink,
        SKPaint outer)
    {
        var area = OmrFormLayout.Pixels(block.Area);
        canvas.DrawRect(Rect(area), outer);
        var questions = block.Rows.Count / 4;
        var slot = area.Width / (float)Math.Max(questions, 1);
        canvas.DrawLine(area.Left, area.Top + block.HeaderHeight, area.Left + area.Width, area.Top + block.HeaderHeight, outer);
        for (var question = 0; question < questions; question++)
        {
            var x = area.Left + question * slot;
            if (question > 0)
                canvas.DrawLine(x, area.Top, x, area.Top + area.Height, outer);
            canvas.DrawText($"Câu {question + 1}", x + slot / 2f, area.Top + 18, SKTextAlign.Center, strongFont, ink);
            canvas.DrawText("Đúng", x + slot * 0.38f, area.Top + 38, SKTextAlign.Center, strongFont, ink);
            canvas.DrawText("Sai", x + slot * 0.72f, area.Top + 38, SKTextAlign.Center, strongFont, ink);
        }

        for (var line = 0; line < 4; line++)
        {
            var row = block.Rows[line];
            var y = area.Top + block.HeaderHeight + line * block.RowHeight + block.RowHeight / 2f + 5;
            canvas.DrawText(row.Label, area.Left + 16, y, SKTextAlign.Left, strongFont, ink);
        }
    }

    private static void Mark(SKCanvas canvas, SKPaint paint, float x, float y) =>
        canvas.DrawRect(SKRect.Create(x, y, 12, 12), paint);

    private static void DrawProctor(SKCanvas canvas, (int Left, int Top, int Width, int Height) box, string line, SKFont font, SKPaint ink)
    {
        var center = box.Left + box.Width / 2f;
        var middle = box.Top + box.Height / 2f;
        canvas.DrawText("Họ tên, chữ ký", center, middle - 6, SKTextAlign.Center, font, ink);
        canvas.DrawText(line, center, middle + 12, SKTextAlign.Center, font, ink);
    }

    // Chữ ký ghi sát mép trên ô, phần dưới để trống.
    private static void DrawSignLabel(SKCanvas canvas, (int Left, int Top, int Width, int Height) box, string line, SKFont font, SKPaint ink)
    {
        var center = box.Left + box.Width / 2f;
        canvas.DrawText("Họ tên, chữ ký", center, box.Top + 28, SKTextAlign.Center, font, ink);
        canvas.DrawText(line, center, box.Top + 50, SKTextAlign.Center, font, ink);
    }

    // Ngày viết theo ba đoạn ngày, tháng, năm, cách nhau bằng gạch chéo.
    private static void DrawDateField(
        SKCanvas canvas,
        OmrFormLayout.LineField field,
        SKFont font,
        SKPaint ink,
        SKPaint dots,
        bool yearPrefix,
        string? tail)
    {
        var box = OmrFormLayout.Pixels(field.Area);
        var baseline = box.Top + box.Height - 2f;
        canvas.DrawText(field.Label, box.Left, baseline, SKTextAlign.Left, font, ink);
        var x = box.Left + font.MeasureText(field.Label) + 6;
        var tailWidth = string.IsNullOrEmpty(tail) ? 0 : font.MeasureText(tail) + 4;
        var right = box.Left + box.Width - tailWidth;
        const string slash = "/";
        var slashWidth = font.MeasureText(slash);
        var prefix = yearPrefix ? "20" : "";
        var prefixWidth = prefix.Length == 0 ? 0 : font.MeasureText(prefix);
        var segment = (right - x - slashWidth * 2 - prefixWidth - 16) / 3f;
        if (segment < 8)
            return;
        void Dots(float from, float to) => canvas.DrawLine(from, baseline + 3, to, baseline + 3, dots);
        Dots(x, x + segment);
        x += segment + 4;
        canvas.DrawText(slash, x, baseline, SKTextAlign.Left, font, ink);
        x += slashWidth + 4;
        Dots(x, x + segment);
        x += segment + 4;
        canvas.DrawText(slash, x, baseline, SKTextAlign.Left, font, ink);
        x += slashWidth + 4;
        if (prefixWidth > 0)
        {
            canvas.DrawText(prefix, x, baseline, SKTextAlign.Left, font, ink);
            x += prefixWidth;
        }

        Dots(x, x + segment);
        if (tailWidth > 0)
            canvas.DrawText(tail!, box.Left + box.Width, baseline, SKTextAlign.Right, font, ink);
    }

    // Lưới số báo danh và mã đề: ô viết phía trên, chữ số nằm giữa vòng tròn.
    private static void DrawMinistryCode(
        SKCanvas canvas,
        OmrFormLayout.CodeGrid grid,
        SKFont strongFont,
        SKFont smallFont,
        SKPaint ink,
        SKPaint rose,
        string? studentCode,
        string? examCode)
    {
        var area = OmrFormLayout.Pixels(grid.Area);
        var across = grid.Bubbles.Where(item => item.Digit == 0).OrderBy(item => item.Column).Select(item => OmrFormLayout.Pixels(item.Area)).ToList();
        var down = grid.Bubbles.Where(item => item.Column == 0).OrderBy(item => item.Digit).Select(item => OmrFormLayout.Pixels(item.Area)).ToList();
        if (across.Count < 2 || down.Count < 2)
            return;
        var pitch = across[1].Left - across[0].Left;
        var rowPitch = down[1].Top - down[0].Top;
        var origin = across[0].Left + across[0].Width / 2f - pitch / 2f;
        var gridTop = down[0].Top + down[0].Height / 2f - rowPitch / 2f;
        using var titleFont = new SKFont(strongFont.Typeface, 16);
        using var digitFont = new SKFont(smallFont.Typeface, 11);
        using var digitInk = new SKPaint { Color = rose.Color, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawText(grid.Title, origin, area.Top - 8, SKTextAlign.Left, titleFont, ink);
        for (var column = 0; column <= grid.Columns; column++)
        {
            var x = origin + column * pitch;
            canvas.DrawLine(x, area.Top, x, area.Top + grid.HeaderHeight, rose);
        }

        canvas.DrawLine(origin, area.Top, origin + grid.Columns * pitch, area.Top, rose);
        canvas.DrawLine(origin, area.Top + grid.HeaderHeight, origin + grid.Columns * pitch, area.Top + grid.HeaderHeight, rose);
        for (var column = 0; column <= grid.Columns; column++)
        {
            var x = origin + column * pitch;
            canvas.DrawLine(x, gridTop, x, gridTop + 10 * rowPitch, rose);
        }

        for (var row = 0; row <= 10; row++)
        {
            var y = gridTop + row * rowPitch;
            canvas.DrawLine(origin, y, origin + grid.Columns * pitch, y, rose);
        }

        foreach (var bubble in grid.Bubbles)
        {
            var box = OmrFormLayout.Pixels(bubble.Area);
            var cx = box.Left + box.Width / 2f;
            var cy = box.Top + box.Height / 2f;
            canvas.DrawOval(SKRect.Create(box.Left, box.Top, box.Width, box.Height), rose);
            digitFont.MeasureText(bubble.Digit.ToString(), out var bounds);
            canvas.DrawText(bubble.Digit.ToString(), cx, cy - bounds.MidY, SKTextAlign.Center, digitFont, digitInk);
        }

        var source = grid.Title.Contains("báo danh", StringComparison.OrdinalIgnoreCase) ? studentCode : examCode;
        if (!TryDigits(source, grid.Columns, out var digits))
            return;
        using var fill = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill, IsAntialias = false };
        for (var column = 0; column < digits.Length; column++)
        {
            var chosen = grid.Bubbles.First(item => item.Column == column && item.Digit == digits[column]);
            canvas.DrawOval(Rect(OmrFormLayout.Pixels(chosen.Area)), fill);
        }
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

    // Phần II: hai câu đúng/sai chung một khung, nét chia ở giữa, nền trắng.
    private static void DrawMinistryTruth(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKFont font,
        SKPaint ink,
        SKPaint rose,
        SKPaint square,
        SKPaint paper)
    {
        var index = 0;
        while (index < plan.Truths.Count)
        {
            var block = plan.Truths[index];
            var frame = OmrFormLayout.Pixels(block.Area);
            var paired = index + 1 < plan.Truths.Count ? plan.Truths[index + 1] : default;
            var right = OmrFormLayout.Pixels(paired.Area);
            var together = paired.Rows is { Count: > 0 } && right.Top == frame.Top && right.Left > frame.Left;
            var edge = together ? right : frame;
            var box = SKRect.Create(frame.Left, frame.Top, edge.Left + edge.Width - frame.Left, frame.Height);
            canvas.DrawRect(box, paper);
            canvas.DrawRect(box, rose);
            canvas.DrawRect(SKRect.Create(frame.Left + 6, frame.Top + 6, 9, 9), square);
            if (together)
                canvas.DrawLine(right.Left, frame.Top, right.Left, frame.Top + frame.Height, rose);
            DrawTruthHalf(canvas, plan, block, frame, font, ink);
            if (together)
                DrawTruthHalf(canvas, plan, paired, right, font, ink);
            index += together ? 2 : 1;
        }
    }

    private static void DrawTruthHalf(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        OmrFormLayout.TruthBlock block,
        (int Left, int Top, int Width, int Height) frame,
        SKFont font,
        SKPaint ink)
    {
        canvas.DrawText(block.Title, frame.Left + frame.Width / 2f, frame.Top + 18, SKTextAlign.Center, font, ink);
        var first = plan.Bubbles.Where(slot => slot.QuestionNumber == block.Rows[0].QuestionNumber).OrderBy(slot => slot.ChoiceIndex).ToList();
        if (first.Count == 2)
        {
            var dung = OmrFormLayout.Pixels(first[0].Area);
            var sai = OmrFormLayout.Pixels(first[1].Area);
            canvas.DrawText("Đúng", dung.Left + dung.Width / 2f, frame.Top + block.HeaderHeight - 8, SKTextAlign.Center, font, ink);
            canvas.DrawText("Sai", sai.Left + sai.Width / 2f, frame.Top + block.HeaderHeight - 8, SKTextAlign.Center, font, ink);
        }

        for (var line = 0; line < block.Rows.Count; line++)
        {
            var row = plan.Bubbles.First(slot => slot.QuestionNumber == block.Rows[line].QuestionNumber && slot.ChoiceIndex == 0);
            var bubble = OmrFormLayout.Pixels(row.Area);
            canvas.DrawText(block.Rows[line].Label + ")", bubble.Left - 6, bubble.Top + bubble.Height / 2f + 4, SKTextAlign.Right, font, ink);
        }
    }

    // Phần III: ô trừ, ô phẩy và hàng chữ số đúng vị trí trên mẫu, không kẻ lưới ô trống.
    private static void DrawMinistryDigits(
        SKCanvas canvas,
        OmrFormLayout.Plan plan,
        SKFont font,
        SKPaint ink,
        SKPaint rose,
        SKPaint hair,
        SKPaint square,
        SKPaint paper)
    {
        foreach (var block in plan.Digits)
        {
            var frame = OmrFormLayout.Pixels(block.Area);
            canvas.DrawRect(Rect(frame), paper);
            canvas.DrawRect(Rect(frame), rose);
            canvas.DrawRect(SKRect.Create(frame.Left + 5, frame.Top + 6, 9, 9), square);
            canvas.DrawText(block.Title, frame.Left + frame.Width / 2f, frame.Top + 18, SKTextAlign.Center, font, ink);
            canvas.DrawLine(frame.Left, frame.Top + block.HeaderHeight - 4, frame.Left + frame.Width, frame.Top + block.HeaderHeight - 4, hair);
            var anchor = plan.Bubbles.First(slot => slot.QuestionNumber == block.QuestionNumber && slot.ChoiceIndex == OmrDigitGrid.Index(0, 2));
            var anchorBox = OmrFormLayout.Pixels(anchor.Area);
            for (var row = 0; row < OmrDigitGrid.Rows; row++)
            {
                var sample = plan.Bubbles.First(slot => slot.QuestionNumber == block.QuestionNumber && slot.ChoiceIndex % OmrDigitGrid.Rows == row);
                var mark = OmrFormLayout.Pixels(sample.Area);
                var symbol = OmrDigitGrid.Symbol(row);
                font.MeasureText(symbol, out var bounds);
                canvas.DrawText(symbol, anchorBox.Left - 6, mark.Top + mark.Height / 2f - bounds.MidY, SKTextAlign.Right, font, ink);
            }
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
        SKPaint square,
        SKPaint? paper = null)
    {
        foreach (var column in plan.Columns)
        {
            var frame = OmrFormLayout.Pixels(column.Area);
            if (paper is not null)
                canvas.DrawRect(Rect(frame), paper);
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
        SKPaint square,
        SKPaint? paper = null)
    {
        foreach (var block in plan.Truths)
        {
            var frame = OmrFormLayout.Pixels(block.Area);
            if (paper is not null)
                canvas.DrawRect(Rect(frame), paper);
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
        SKPaint square,
        SKPaint? paper = null)
    {
        foreach (var block in plan.Digits)
        {
            var frame = OmrFormLayout.Pixels(block.Area);
            if (paper is not null)
                canvas.DrawRect(Rect(frame), paper);
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
