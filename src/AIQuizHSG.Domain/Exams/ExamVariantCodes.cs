namespace AIQuizHSG.Domain.Exams;

// Phân tích chuỗi mã đề kiểu Azota: "101-103" hoặc "111,222,333".
public static class ExamVariantCodes
{
    public static bool TryParse(string? text, int count, out IReadOnlyList<string> codes, out string? error)
    {
        codes = [];
        error = null;
        if (count < 1 || count > 40)
        {
            error = "Số mã đề từ 1 đến 40.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            codes = Enumerable.Range(1, count).Select(item => item.ToString().PadLeft(3, '0')).ToList();
            return true;
        }

        var raw = text.Trim();
        if (raw.Contains('-', StringComparison.Ordinal) && !raw.Contains(',', StringComparison.Ordinal))
        {
            var parts = raw.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var start) || !int.TryParse(parts[1], out var end) || end < start)
            {
                error = "Khoảng mã đề không hợp lệ. Ví dụ: 034-036.";
                return false;
            }

            var width = Math.Max(parts[0].Length, parts[1].Length);
            var span = end - start + 1;
            if (span != count)
            {
                error = $"Khoảng mã tạo {span} mã nhưng số đề đảo là {count}.";
                return false;
            }

            codes = Enumerable.Range(start, span).Select(item => item.ToString().PadLeft(width, '0')).ToList();
            return true;
        }

        var list = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (list.Count != count)
        {
            error = $"Đã nhập {list.Count} mã nhưng số đề đảo là {count}.";
            return false;
        }

        if (list.Any(item => item.Length == 0 || item.Any(ch => !char.IsDigit(ch))))
        {
            error = "Mỗi mã đề chỉ gồm chữ số.";
            return false;
        }

        if (list.Distinct(StringComparer.Ordinal).Count() != list.Count)
        {
            error = "Các mã đề không được trùng nhau.";
            return false;
        }

        codes = list;
        return true;
    }

    // Mã tương thích đề cũ DE{id}: lấy phần số.
    public static string LegacyCode(long examId)
    {
        var digits = examId.ToString();
        return digits.Length >= 3 ? digits : digits.PadLeft(3, '0');
    }
}
