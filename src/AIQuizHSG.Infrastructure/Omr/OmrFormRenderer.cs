using AIQuizHSG.Application.Omr;

namespace AIQuizHSG.Infrastructure.Omr;

public sealed class OmrFormRenderer : IOmrFormRenderer
{
    public byte[] Png(OmrTemplateConfig config) => OmrFormImage.Png(config);
}