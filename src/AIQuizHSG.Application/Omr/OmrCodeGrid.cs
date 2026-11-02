namespace AIQuizHSG.Application.Omr;

// Số báo danh và mã đề tô theo cột. Cột trống ở hai đầu thì bỏ, cột trống xen giữa thì không đọc.
public static class OmrCodeGrid
{
    public readonly record struct Column(int Index, int? Digit, bool Ambiguous);

    public static bool TryRead(IReadOnlyList<Column> columns, out string text)
    {
        text = "";
        if (columns.Count == 0 || columns.Any(item => item.Ambiguous))
            return false;

        var ordered = columns.OrderBy(item => item.Index).ToList();
        var first = ordered.FindIndex(item => item.Digit is not null);
        var last = ordered.FindLastIndex(item => item.Digit is not null);
        if (first < 0)
            return true;

        var digits = new char[last - first + 1];
        for (var index = first; index <= last; index++)
        {
            if (ordered[index].Digit is not int digit)
                return false;
            digits[index - first] = (char)('0' + digit);
        }

        text = new string(digits);
        return true;
    }
}
