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
        using var bitmap = Decode(image);
        if (bitmap is null)
            return new OmrRecognitionResult { Error = "Không đọc được ảnh phiếu." };

        var qr = ReadQr(bitmap);
        var found = OmrRules.TryReadQr(qr, out var qrStudent, out var qrExam);
        ReadCodes(bitmap, config, out var bubbledStudent, out var bubbledExam);
        var bubbles = new List<OmrBubbleReading>(config.Bubbles.Count);
        foreach (var bubble in config.Bubbles)
        {
            var ratio = DarkRatio(bitmap, bubble);
            var status = ratio >= config.MarkedRatio
                ? nameof(OmrMarkStatus.Marked)
                : ratio <= config.BlankRatio ? nameof(OmrMarkStatus.Blank) : nameof(OmrMarkStatus.Ambiguous);
            bubbles.Add(new OmrBubbleReading
            {
                QuestionNumber = bubble.QuestionNumber,
                ChoiceIndex = bubble.ChoiceIndex,
                Ratio = ratio,
                Status = status
            });
        }

        return new OmrRecognitionResult
        {
            StudentCode = string.IsNullOrWhiteSpace(qrStudent) ? bubbledStudent : qrStudent,
            ExamCode = string.IsNullOrWhiteSpace(qrExam) ? bubbledExam : qrExam,
            QrFound = found,
            Bubbles = bubbles
        };
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
            foreach (var column in grid.Bubbles.GroupBy(item => item.Column))
            {
                var cells = column.Select(item => (item.Digit, Ratio: DarkRatio(image, new OmrBubbleConfig
                {
                    X = item.Area.X,
                    Y = item.Area.Y,
                    Width = item.Area.Width,
                    Height = item.Area.Height
                }))).ToList();
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
        var ordered = cells.OrderByDescending(item => item.Ratio).ToList();
        var top = ordered[0];
        var next = ordered.Count > 1 ? ordered[1].Ratio : 0;
        if (top.Ratio >= marked && next >= marked)
            return new OmrCodeGrid.Column(column, null, true);
        if (top.Ratio >= marked && top.Ratio - next >= 0.25)
            return new OmrCodeGrid.Column(column, top.Digit, false);
        if (top.Ratio >= marked)
            return new OmrCodeGrid.Column(column, null, true);
        // Cột không có ô tô đậm, các vòng in sẵn đều nhau, thì coi là bỏ trống.
        if (top.Ratio - next < 0.12)
            return new OmrCodeGrid.Column(column, null, false);
        return new OmrCodeGrid.Column(column, null, true);
    }

    private static double DarkRatio(SKBitmap image, OmrBubbleConfig bubble)
    {
        var left = Clamp((int)Math.Round(bubble.X * image.Width), 0, image.Width - 1);
        var top = Clamp((int)Math.Round(bubble.Y * image.Height), 0, image.Height - 1);
        var right = Clamp(left + Math.Max(1, (int)Math.Round(bubble.Width * image.Width)), left + 1, image.Width);
        var bottom = Clamp(top + Math.Max(1, (int)Math.Round(bubble.Height * image.Height)), top + 1, image.Height);
        var dark = 0;
        var total = 0;
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                total++;
                var pixel = image.GetPixel(x, y);
                var luminance = pixel.Red * 0.299 + pixel.Green * 0.587 + pixel.Blue * 0.114;
                if (luminance < 140)
                    dark++;
            }
        }

        return total == 0 ? 0 : (double)dark / total;
    }

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
