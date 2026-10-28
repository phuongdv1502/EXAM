using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Omr;

// Quy tắc trạng thái phiếu, tách khỏi API để kiểm thử được.
public static class OmrRules
{
    public const string QrPrefix = "HSG";

    public static bool CanGrade(OmrSheetStatus status) =>
        status is OmrSheetStatus.Recognized or OmrSheetStatus.Confirmed;

    public static bool IsSettled(OmrSheetStatus status) => status == OmrSheetStatus.Graded;

    // Công bố chỉ khi mọi phiếu đã chấm. Phiếu lỗi hoặc còn chờ duyệt thì chặn.
    public static bool CanApprove(IReadOnlyCollection<OmrSheetStatus> statuses) =>
        statuses.Count > 0 && statuses.All(IsSettled);

    public static string QrPayload(string studentCode, string examCode) =>
        $"{QrPrefix}|{studentCode.Trim()}|{examCode.Trim()}";

    public static bool TryReadQr(string? text, out string studentCode, out string examCode)
    {
        studentCode = string.Empty;
        examCode = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Trim().Split('|');
        if (parts.Length != 3 || !string.Equals(parts[0], QrPrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
            return false;

        studentCode = parts[1].Trim();
        examCode = parts[2].Trim();
        return true;
    }
}
