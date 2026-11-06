using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Omr;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace AIQuizHSG.Infrastructure.Omr;

public sealed class OmrRecognitionService : IOmrRecognitionService
{
    public OmrRecognitionResult Recognize(ReadOnlyMemory<byte> image, OmrTemplateConfig config)
    {
        using var decoded = Decode(image);
        if (decoded is null)
            return new OmrRecognitionResult { Error = "Không đọc được ảnh phiếu." };

        // Ảnh chụp tay còn nền xung quanh. Kéo tờ phiếu về trang A4 rồi mới đọc ô số báo danh.
        var page = OmrPageAlign.Straighten(decoded);
        // Sau khi kéo mép giấy, canh lại theo ô mốc cột. Scan máy in còn lề trắng dưới tờ.
        var fitted = OmrPageAlign.AlignForm(page, config);
        if (!ReferenceEquals(fitted, page))
        {
            if (!ReferenceEquals(page, decoded))
                page.Dispose();
            page = fitted;
        }

        try
        {
        var qr = ReadQr(page);
        var found = OmrRules.TryReadQr(qr, out var qrStudent, out var qrExam);
        ReadCodes(page, config, out var bubbledStudent, out var bubbledExam);
        // Ảnh chụp tay lệch so với mẫu. Số báo danh đã tự canh. Cột đáp án bám mép khung của chính cột đó.
        var answerShift = AlignAnswers(page, config);
        var measured = new List<(OmrBubbleConfig Bubble, double Ratio)>(config.Bubbles.Count);
        foreach (var bubble in config.Bubbles)
        {
            answerShift.TryGetValue(bubble.QuestionNumber, out var nudge);
            measured.Add((bubble, DarkRatio(page, bubble, nudge)));
        }

        // So các ô trong cùng một câu. Ô trội hơn hẳn thì nhận, hai ô cùng đậm thì không đoán.
        var bubbles = new List<OmrBubbleReading>(measured.Count);
        foreach (var group in measured.GroupBy(item => GroupKey(config, item.Bubble)))
        {
            var cells = group.ToList();
            var decision = OmrMarkChoice.Choose(cells.Select(item => item.Ratio).ToList(), config.MarkedRatio, config.BlankRatio);
            for (var index = 0; index < cells.Count; index++)
            {
                var chosen = decision.Status == OmrMarkStatus.Marked && decision.Index == index;
                var status = chosen
                    ? nameof(OmrMarkStatus.Marked)
                    : decision.Status == OmrMarkStatus.Ambiguous && cells[index].Ratio >= config.BlankRatio
                        ? nameof(OmrMarkStatus.Ambiguous)
                        : nameof(OmrMarkStatus.Blank);
                bubbles.Add(new OmrBubbleReading
                {
                    QuestionNumber = cells[index].Bubble.QuestionNumber,
                    ChoiceIndex = cells[index].Bubble.ChoiceIndex,
                    Ratio = cells[index].Ratio,
                    Status = status
                });
            }
        }

        bubbles.Sort((left, right) =>
        {
            var byQuestion = left.QuestionNumber.CompareTo(right.QuestionNumber);
            return byQuestion != 0 ? byQuestion : left.ChoiceIndex.CompareTo(right.ChoiceIndex);
        });

        return new OmrRecognitionResult
        {
            StudentCode = string.IsNullOrWhiteSpace(qrStudent) ? bubbledStudent : qrStudent,
            ExamCode = string.IsNullOrWhiteSpace(qrExam) ? bubbledExam : qrExam,
            QrFound = found,
            Bubbles = bubbles
        };
        }
        finally
        {
            if (!ReferenceEquals(page, decoded))
                page.Dispose();
        }
    }

    private static SKBitmap? Decode(ReadOnlyMemory<byte> image)
    {
        using var data = SKData.CreateCopy(image.Span);
        using var codec = SKCodec.Create(data);
        if (codec is null)
            return null;
        var info = codec.Info;
        var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
        {
            bitmap.Dispose();
            return null;
        }

        return Orient(bitmap, codec.EncodedOrigin);
    }

    private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.LeftTop)
            return source;

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var width = swap ? source.Height : source.Width;
        var height = swap ? source.Width : source.Height;
        var oriented = new SKBitmap(width, height);
        using var canvas = new SKCanvas(oriented);
        canvas.Clear(SKColors.White);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight:
                canvas.Scale(-1, 1);
                canvas.Translate(-source.Width, 0);
                break;
            case SKEncodedOrigin.BottomRight:
                canvas.RotateDegrees(180, source.Width / 2f, source.Height / 2f);
                break;
            case SKEncodedOrigin.BottomLeft:
                canvas.Scale(1, -1);
                canvas.Translate(0, -source.Height);
                break;
            case SKEncodedOrigin.RightTop:
                canvas.Translate(source.Height, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:
                canvas.Translate(source.Height, source.Width);
                canvas.RotateDegrees(90);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.LeftBottom:
                canvas.Translate(0, source.Width);
                canvas.RotateDegrees(-90);
                break;
            case SKEncodedOrigin.LeftTop:
                canvas.Translate(0, 0);
                canvas.RotateDegrees(-90);
                canvas.Scale(-1, 1);
                break;
        }

        canvas.DrawBitmap(source, 0, 0);
        source.Dispose();
        return oriented;
    }

    // QR có thì dùng. Không có thì đọc ô tô số báo danh và mã đề trên phiếu.
    private readonly record struct NudgeShift(double X, double Y);

    // Méo giấy làm cột đáp án lệch khác lưới số báo danh. Lấy mép trên của khung cột rồi dịch cả cột theo mép đó.
    private static Dictionary<int, NudgeShift> AlignAnswers(SKBitmap image, OmrTemplateConfig config)
    {
        var shifts = new Dictionary<int, NudgeShift>();
        var plan = OmrFormLayout.For(config);
        foreach (var column in plan.Columns)
        {
            var shift = AlignFrame(image, OmrFormLayout.Pixels(column.Area));
            foreach (var number in column.Questions)
            {
                if (number > 0)
                    shifts[number] = shift;
            }
        }

        foreach (var block in plan.Truths)
        {
            var shift = AlignFrame(image, OmrFormLayout.Pixels(block.Area));
            foreach (var row in block.Rows)
                shifts[row.QuestionNumber] = shift;
        }

        return shifts;
    }

    private static NudgeShift AlignFrame(SKBitmap image, (int Left, int Top, int Width, int Height) frame)
    {
        var x0 = Math.Max(0, frame.Left - 36);
        var x1 = Math.Min(image.Width - 1, frame.Left + frame.Width + 36);
        var span = Math.Max(1, x1 - x0);
        var bestY = -1;
        var best = 0;
        var yStart = Math.Max(0, frame.Top - 28);
        // Khung in có thể thấp hơn mẫu khoảng một hàng sau khi kéo trang.
        var yEnd = Math.Min(image.Height - 1, frame.Top + 40);
        for (var y = yStart; y <= yEnd; y++)
        {
            var dark = 0;
            for (var x = x0; x <= x1; x++)
            {
                if (Ink(image, x, y))
                    dark++;
            }

            if (dark <= best)
                continue;
            best = dark;
            bestY = y;
        }

        // Không thấy mép khung thì giữ đúng tọa độ mẫu, tránh nhảy sang ô bên cạnh.
        if (bestY < 0 || best < span * 0.45)
            return default;

        // Đoạn kẻ trùng giữa cạnh trên của khung. Chữ A–D và ô tô không kéo mép trái.
        var left = frame.Left;
        var innerLeft = frame.Left + frame.Width / 5;
        var innerRight = frame.Left + frame.Width * 4 / 5;
        var runStart = -1;
        var runLength = 0;
        var bestStart = frame.Left;
        var bestLength = 0;
        void ConsiderRun()
        {
            if (runStart < 0 || runLength <= bestLength)
                return;
            var runEnd = runStart + runLength - 1;
            if (runEnd < innerLeft || runStart > innerRight)
                return;
            bestLength = runLength;
            bestStart = runStart;
        }

        for (var x = x0; x <= x1; x++)
        {
            if (Ink(image, x, bestY))
            {
                if (runStart < 0)
                    runStart = x;
                runLength++;
                continue;
            }

            ConsiderRun();
            runStart = -1;
            runLength = 0;
        }

        ConsiderRun();
        // Đoạn kẻ bắt đầu lệch vào trong là hàng chữ A–D, không phải mép khung.
        if (bestLength >= span * 0.35 && bestStart <= frame.Left + 12)
            left = bestStart;

        var dx = left - frame.Left;
        var dy = bestY - frame.Top;
        // Ảnh chụp nghiêng hoặc scan còn lề trắng có thể đẩy cột lệch hơn một hàng.
        if (Math.Abs(dx) > 72)
            dx = 0;
        if (Math.Abs(dy) > 48)
            return default;
        return new NudgeShift(dx / (double)image.Width, dy / (double)image.Height);
    }

    private static bool Ink(SKBitmap image, int x, int y)
    {
        var pixel = image.GetPixel(x, y);
        return pixel.Red * 0.299 + pixel.Green * 0.587 + pixel.Blue * 0.114 < 150;
    }

    private static NudgeShift NudgeGrid(SKBitmap image, OmrFormLayout.CodeGrid grid, double marked)
    {
        var best = new NudgeShift(0, 0);
        var bestScore = Contrast(image, grid, best, marked);
        // Chỉ dò trong nửa ô. Dò xa hơn thì cả lưới trượt sang hàng bên cạnh và đọc sai mọi chữ số.
        var cellWidth = (grid.Area.Width * OmrFormLayout.PageWidth - grid.LabelWidth) / Math.Max(1, grid.Columns);
        var reachY = Math.Min(0.028, grid.Cell * 0.45 / OmrFormLayout.PageHeight);
        var reachX = Math.Min(0.016, cellWidth * 0.45 / OmrFormLayout.PageWidth);
        for (var y = -reachY; y <= reachY + 1e-9; y += reachY / 5)
        {
            for (var x = -reachX; x <= reachX + 1e-9; x += reachX / 4)
            {
                var shift = new NudgeShift(x, y);
                var score = Contrast(image, grid, shift, marked);
                if (score <= bestScore)
                    continue;
                bestScore = score;
                best = shift;
            }
        }

        return best;
    }

    private static double Contrast(SKBitmap image, OmrFormLayout.CodeGrid grid, NudgeShift shift, double marked)
    {
        var score = 0d;
        foreach (var column in grid.Bubbles.GroupBy(item => item.Column))
        {
            var ratios = column.Select(item => DarkRatio(image, new OmrBubbleConfig
            {
                X = item.Area.X,
                Y = item.Area.Y,
                Width = item.Area.Width,
                Height = item.Area.Height
            }, shift)).OrderByDescending(item => item).ToList();
            if (ratios.Count == 0 || ratios[0] < marked)
                continue;
            var next = ratios.Count > 1 ? ratios[1] : 0;
            score += ratios[0] - next;
        }

        return score;
    }

    private static void ReadCodes(SKBitmap image, OmrTemplateConfig config, out string? studentCode, out string? examCode)
    {
        studentCode = null;
        examCode = null;
        foreach (var grid in OmrFormLayout.For(config).Codes)
        {
            var role = grid.Title.Contains("báo danh", StringComparison.OrdinalIgnoreCase) ? "student"
                : grid.Title.Contains("mã đề", StringComparison.OrdinalIgnoreCase) ? "exam"
                : null;
            if (role is null)
                continue;

            var columns = new List<OmrCodeGrid.Column>();
            var nudge = NudgeGrid(image, grid, config.MarkedRatio);
            foreach (var column in grid.Bubbles.GroupBy(item => item.Column))
            {
                var cells = column.Select(item => (item.Digit, Ratio: DarkRatio(image, new OmrBubbleConfig
                {
                    X = item.Area.X,
                    Y = item.Area.Y,
                    Width = item.Area.Width,
                    Height = item.Area.Height
                }, nudge))).ToList();
                columns.Add(ReadColumn(column.Key, cells, config.MarkedRatio));
            }

            if (!OmrCodeGrid.TryRead(columns, out var text) || text.Length == 0)
                continue;
            if (role == "student")
                studentCode = text;
            else
                examCode = text;
        }
    }

    // Ô tô đậm hơn hẳn các ô còn lại trong cột thì lấy chữ số đó. Chữ in mờ trong ô không tính là tô.
    private static OmrCodeGrid.Column ReadColumn(int column, List<(int Digit, double Ratio)> cells, double marked)
    {
        if (cells.Count == 0)
            return new OmrCodeGrid.Column(column, null, true);
        var decision = OmrMarkChoice.Choose(cells.Select(item => item.Ratio).ToList(), marked);
        if (decision.Status == OmrMarkStatus.Marked && decision.Index is int index)
            return new OmrCodeGrid.Column(column, cells[index].Digit, false);
        if (decision.Status == OmrMarkStatus.Blank)
            return new OmrCodeGrid.Column(column, null, false);
        return new OmrCodeGrid.Column(column, null, true);
    }

    // Điền số thì so trong từng cột chữ số. Câu trắc nghiệm so các lựa chọn của câu đó.
    private static string GroupKey(OmrTemplateConfig config, OmrBubbleConfig bubble)
    {
        if (config.KindOf(bubble.QuestionNumber) == OmrSectionKinds.DienSo)
            return $"d{bubble.QuestionNumber}:{bubble.ChoiceIndex / OmrDigitGrid.Rows}";
        return $"q{bubble.QuestionNumber}";
    }

    private static double DarkRatio(SKBitmap image, OmrBubbleConfig bubble, NudgeShift nudge = default)
    {
        // Lấy lòng ô, bỏ vòng in sẵn. Nudge dịch cả lưới khi ảnh lệch nửa hàng.
        var boxLeft = Clamp((int)Math.Round((bubble.X + nudge.X) * image.Width), 0, image.Width - 1);
        var boxTop = Clamp((int)Math.Round((bubble.Y + nudge.Y) * image.Height), 0, image.Height - 1);
        var boxRight = Clamp(boxLeft + Math.Max(1, (int)Math.Round(bubble.Width * image.Width)), boxLeft + 1, image.Width);
        var boxBottom = Clamp(boxTop + Math.Max(1, (int)Math.Round(bubble.Height * image.Height)), boxTop + 1, image.Height);
        var left = Clamp((int)Math.Round((bubble.X + nudge.X + bubble.Width * 0.22) * image.Width), 0, image.Width - 1);
        var top = Clamp((int)Math.Round((bubble.Y + nudge.Y + bubble.Height * 0.22) * image.Height), 0, image.Height - 1);
        var right = Clamp(left + Math.Max(1, (int)Math.Round(bubble.Width * 0.56 * image.Width)), left + 1, image.Width);
        var bottom = Clamp(top + Math.Max(1, (int)Math.Round(bubble.Height * 0.56 * image.Height)), top + 1, image.Height);
        // Nền giấy quanh ô quyết định thế nào là mực. Ảnh tối hoặc bút chì nhạt không bị một ngưỡng cố định làm sai.
        var limit = InkLimit(PaperLevel(image, boxLeft, boxTop, boxRight, boxBottom));
        var dark = 0;
        var total = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                total++;
                if (Luma(image.GetPixel(x, y)) < limit)
                    dark++;
            }
        }

        return total == 0 ? 0 : (double)dark / total;
    }

    // Phần sáng quanh ô là giấy. Vòng in sẵn và ô bên cạnh đậm không được kéo ngưỡng xuống.
    private static double PaperLevel(SKBitmap image, int left, int top, int right, int bottom)
    {
        var padX = Math.Max(2, (right - left) / 3);
        var padY = Math.Max(2, (bottom - top) / 3);
        var x0 = Clamp(left - padX, 0, image.Width - 1);
        var y0 = Clamp(top - padY, 0, image.Height - 1);
        var x1 = Clamp(right + padX, x0 + 1, image.Width);
        var y1 = Clamp(bottom + padY, y0 + 1, image.Height);
        var levels = new List<int>();
        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
            {
                if (x >= left && x < right && y >= top && y < bottom)
                    continue;
                levels.Add(Luma(image.GetPixel(x, y)));
            }
        }

        if (levels.Count < 8)
            return 220;
        // Chỉ lấy các điểm sáng sát mức sáng nhất. Ô tô bên cạnh không được coi là giấy.
        var peak = 0;
        foreach (var level in levels)
        {
            if (level > peak)
                peak = level;
        }

        var sum = 0;
        var count = 0;
        foreach (var level in levels)
        {
            if (level < peak - 25)
                continue;
            sum += level;
            count++;
        }

        return count == 0 ? peak : sum / (double)count;
    }

    // Giấy sáng giữ ngưỡng cũ. Giấy tối thì hạ ngưỡng để nền xám không bị tính là mực.
    private static double InkLimit(double paper) => paper >= 170 ? 140 : Math.Clamp(paper * 0.72, 90, 140);

    private static int Luma(SKColor pixel) => (pixel.Red * 299 + pixel.Green * 587 + pixel.Blue * 114) / 1000;

    // Đọc mã QR trước, để biết phiếu thuộc đề nào rồi mới chọn mẫu.
    public string? ReadQr(ReadOnlyMemory<byte> image)
    {
        using var bitmap = Decode(image);
        return bitmap is null ? null : ReadQr(bitmap);
    }

    private static string? ReadQr(SKBitmap image)
    {
        var zoneWidth = Math.Max(1, image.Width / 2);
        var zoneHeight = Math.Max(1, image.Height / 3);
        using var zone = new SKBitmap(zoneWidth, zoneHeight);
        using (var canvas = new SKCanvas(zone))
            canvas.DrawBitmap(image, 0, 0);
        return DecodeQr(zone) ?? DecodeQr(image);
    }

    private static string? DecodeQr(SKBitmap image)
    {
        var bytes = new byte[image.Width * image.Height * 3];
        var index = 0;
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image.GetPixel(x, y);
                bytes[index++] = pixel.Red;
                bytes[index++] = pixel.Green;
                bytes[index++] = pixel.Blue;
            }
        }

        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = [BarcodeFormat.QR_CODE]
            }
        };
        var source = new RGBLuminanceSource(bytes, image.Width, image.Height, RGBLuminanceSource.BitmapFormat.RGB24);
        return reader.Decode(source)?.Text;
    }

    private static int Clamp(int value, int min, int max) => Math.Min(max, Math.Max(min, value));
}
