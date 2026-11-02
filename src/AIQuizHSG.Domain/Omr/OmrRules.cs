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

        examCode = parts[2].Trim();
        if (string.IsNullOrWhiteSpace(examCode))
            return false;

        // Dấu gạch là phiếu in trống, chưa có mã học sinh.
        var student = parts[1].Trim();
        studentCode = student == "-" ? string.Empty : student;
        return true;
    }

    // Mã đề tô trên phiếu là chữ số. Đợt chấm lưu dạng DE1, nên so phần số sau khi bỏ số 0 ở đầu.
    public static bool SameExamCode(string? recognized, string expected)
    {
        if (string.IsNullOrWhiteSpace(recognized) || string.IsNullOrWhiteSpace(expected))
            return false;
        if (string.Equals(recognized.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        var left = DigitRun(recognized);
        var right = DigitRun(expected);
        if (left.Length == 0 || right.Length == 0)
            return false;
        return string.Equals(Significant(left), Significant(right), StringComparison.Ordinal);
    }

    public static string DigitRun(string value) => new(value.Where(char.IsDigit).ToArray());

    private static string Significant(string digits)
    {
        var trimmed = digits.TrimStart('0');
        return trimmed.Length == 0 ? "0" : trimmed;
    }
}
