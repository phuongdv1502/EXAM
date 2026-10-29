using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Omr;

// Chọn mẫu khi một mã đề có nhiều đợt. Phiếu đọc được mã và ít ô mơ hồ thì thắng.
public static class OmrScanChoice
{
    public static bool Accepts(OmrRecognitionResult read) =>
        read.Error is null && read.QrFound && read.Bubbles.Count > 0;

    public static int Score(OmrRecognitionResult read)
    {
        var ambiguous = read.Bubbles.Count(item => item.Status == nameof(OmrMarkStatus.Ambiguous));
        return read.Bubbles.Count - ambiguous * 2 + (read.QrFound ? 100 : 0);
    }
}
