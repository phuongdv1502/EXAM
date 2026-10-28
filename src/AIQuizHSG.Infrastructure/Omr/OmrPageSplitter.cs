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
            if (image is not null && image.TryGetPng(out var png) && png.Length > 0)
                pages.Add(new OmrPageImage(page.Number, "image/png", png, null));
            else
                pages.Add(new OmrPageImage(page.Number, contentType, [], "Trang PDF không chứa ảnh phiếu."));
        }

        return pages;
    }
}
