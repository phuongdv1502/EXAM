using AIQuizHSG.Application.Omr;

namespace AIQuizHSG.Infrastructure.Omr;

public sealed class OmrFormRenderer : IOmrFormRenderer
{
    public byte[] Png(OmrTemplateConfig config) => OmrFormImage.Png(config);

    public byte[] Png(OmrTemplateConfig config, OmrSheetCopy copy) => OmrFormImage.Png(config, copy);

    public byte[] Pdf(OmrTemplateConfig config, IReadOnlyList<OmrSheetCopy> copies) => OmrFormImage.Pdf(config, copies);
}