using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Omr;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace AIQuizHSG.Infrastructure.Omr;

// Ảnh phiếu giả để kiểm thử nhận diện, không dùng khi chấm thật.
internal static class OmrSampleImage
{
    public static byte[] Png(OmrTemplateConfig config, string studentCode, string examCode, IReadOnlySet<(int Question, int Choice)> marked)
    {
        using var image = new SKBitmap(1000, 1400);
        using (var canvas = new SKCanvas(image))
            canvas.Clear(SKColors.White);
        DrawQr(image, OmrRules.QrPayload(studentCode, examCode));
        foreach (var bubble in config.Bubbles)
        {
            if (marked.Contains((bubble.QuestionNumber, bubble.ChoiceIndex)))
                Fill(image, bubble);
        }

        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static void DrawQr(SKBitmap image, string payload)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions { Width = 220, Height = 220, Margin = 1 }
        };
        var pixels = writer.Write(payload);
        for (var y = 0; y < pixels.Height; y++)
        {
            for (var x = 0; x < pixels.Width; x++)
            {
                var blue = pixels.Pixels[(y * pixels.Width + x) * 4];
                image.SetPixel(x + 40, y + 40, blue < 128 ? SKColors.Black : SKColors.White);
            }
        }
    }

    private static void Fill(SKBitmap image, OmrBubbleConfig bubble)
    {
        var left = (int)Math.Round(bubble.X * image.Width);
        var top = (int)Math.Round(bubble.Y * image.Height);
        var right = left + Math.Max(1, (int)Math.Round(bubble.Width * image.Width));
        var bottom = top + Math.Max(1, (int)Math.Round(bubble.Height * image.Height));
        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
                image.SetPixel(x, y, SKColors.Black);
        }
    }
}
