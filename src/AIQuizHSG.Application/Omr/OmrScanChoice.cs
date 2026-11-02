using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Omr;

// Chọn mẫu khi một mã đề có nhiều đợt. Phiếu ít ô mơ hồ thì thắng, có QR chỉ là điểm cộng.
public static class OmrScanChoice
{
    public static bool Accepts(OmrRecognitionResult read)
    {
        if (read.Error is not null || read.Bubbles.Count == 0)
            return false;
        var ambiguous = read.Bubbles.Count(item => item.Status == nameof(OmrMarkStatus.Ambiguous));
        return ambiguous * 2 < read.Bubbles.Count;
    }

    public static int Score(OmrRecognitionResult read)
    {
        var ambiguous = read.Bubbles.Count(item => item.Status == nameof(OmrMarkStatus.Ambiguous));
        return read.Bubbles.Count - ambiguous * 2 + (read.QrFound ? 100 : 0);
    }
}
