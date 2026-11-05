using AIQuizHSG.Application.Omr;
using UglyToad.PdfPig;

namespace AIQuizHSG.Infrastructure.Omr;

public sealed class OmrPageSplitter : IOmrPageSplitter
{
    public IReadOnlyList<OmrPageImage> Split(string contentType, ReadOnlyMemory<byte> content)
    {
        if (!contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return [new OmrPageImage(1, contentType, content.ToArray(), null)];

        var pages = new List<OmrPageImage>();
        using var document = PdfDocument.Open(content.ToArray());
        if (document.NumberOfPages == 0)
            return [new OmrPageImage(1, contentType, [], "PDF không có trang.")];

        foreach (var page in document.GetPages())
        {
            var image = page.GetImages()
                .OrderByDescending(item => (long)item.WidthInSamples * item.HeightInSamples)
                .FirstOrDefault();
            // Máy scan Canon nhúng JPEG. Thư viện không ghép được PNG thì giữ nguyên dòng JPEG đó.
            if (image is not null && image.TryGetPng(out var png) && png is { Length: > 0 })
                pages.Add(new OmrPageImage(page.Number, "image/png", png, null));
            else if (image is not null && TryJpeg(image.RawBytes, out var jpeg))
                pages.Add(new OmrPageImage(page.Number, "image/jpeg", jpeg, null));
            else
                pages.Add(new OmrPageImage(page.Number, contentType, [], "Trang PDF không chứa ảnh phiếu."));
        }

        return pages;
    }

    private static bool TryJpeg(ReadOnlySpan<byte> raw, out byte[] jpeg)
    {
        jpeg = [];
        if (raw.Length < 3 || raw[0] != 0xFF || raw[1] != 0xD8 || raw[2] != 0xFF)
            return false;
        jpeg = raw.ToArray();
        return true;
    }
}
