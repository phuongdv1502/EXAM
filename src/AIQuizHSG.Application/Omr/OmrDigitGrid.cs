using System.Text.Json;

namespace AIQuizHSG.Application.Omr;

// Lưới điền số: 4 cột, mỗi cột một ký tự. Hàng là dấu trừ, dấu phẩy, rồi chữ số 0–9.
public static class OmrDigitGrid
{
    public const int Columns = 4;
    public const int Rows = 12;

    public static int Index(int column, int row) => column * Rows + row;

    public static string Symbol(int row) => row switch
    {
        0 => "-",
        1 => ",",
        _ => (row - 2).ToString()
    };

    public static bool SameValue(string? stored, string bubbled)
    {
        static string Norm(string? value) => (value ?? "").Trim().Replace(" ", "", StringComparison.Ordinal).Replace('.', ',');
        var left = Norm(stored);
        var right = Norm(bubbled);
        return left.Length > 0 && left.Equals(right, StringComparison.OrdinalIgnoreCase);
    }

    // Đọc các ô đã lưu. Cột không tô thì bỏ qua. Một cột tô hai ô thì phiếu mơ hồ.
    public static bool TryRead(IEnumerable<(int ChoiceIndex, string Status)> marks, out string text)
    {
        text = "";
        var list = marks.ToList();
        var chars = new List<string>();
        foreach (var column in Enumerable.Range(0, Columns))
        {
            var inColumn = list.Where(item => item.ChoiceIndex / Rows == column).ToList();
            if (inColumn.Any(item => item.Status == "Ambiguous"))
                return false;
            var marked = inColumn.Where(item => item.Status == "Marked").Select(item => item.ChoiceIndex % Rows).Distinct().ToList();
            if (marked.Count > 1)
                return false;
            if (marked.Count == 1)
                chars.Add(Symbol(marked[0]));
        }

        text = string.Concat(chars);
        return true;
    }

    public static string? ReadStored(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            var marks = JsonSerializer.Deserialize<List<StoredBubble>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            return TryRead(marks.Select(item => (item.ChoiceIndex, item.Status ?? "")), out var text) ? text : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class StoredBubble
    {
        public int ChoiceIndex { get; set; }
        public string? Status { get; set; }
    }
}
