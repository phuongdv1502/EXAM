namespace AIQuizHSG.Application.Omr;

// Ba phần trên phiếu nhiều phần, đúng kiểu phiếu trắc nghiệm hiện nay.
public static class OmrSectionKinds
{
    public const string BonDapAn = "BonDapAn";
    public const string DungSai = "DungSai";
    public const string DienSo = "DienSo";

    public static bool TryNormalize(string? kind, out string code)
    {
        code = kind?.Trim() ?? string.Empty;
        if (code.Equals(BonDapAn, StringComparison.OrdinalIgnoreCase))
        {
            code = BonDapAn;
            return true;
        }

        if (code.Equals(DungSai, StringComparison.OrdinalIgnoreCase))
        {
            code = DungSai;
            return true;
        }

        if (code.Equals(DienSo, StringComparison.OrdinalIgnoreCase))
        {
            code = DienSo;
            return true;
        }

        code = string.Empty;
        return false;
    }

    // Đúng/sai: một câu trên phiếu gồm 4 ý, mỗi ý là một câu của đề.
    public static int ItemsPerQuestion(string kind) => kind == DungSai ? 4 : 1;

    public static int BubbleChoices(string kind) => kind switch
    {
        DungSai => 2,
        DienSo => OmrDigitGrid.Columns * OmrDigitGrid.Rows,
        _ => 4
    };

    public static bool CountAllowed(string kind, int count) => kind switch
    {
        BonDapAn => count is >= 1 and <= 40,
        DungSai => count is >= 1 and <= 8,
        DienSo => count is >= 1 and <= 6,
        _ => false
    };

    // Phần III phiếu tự luận: dấu trừ chỉ cột đầu, dấu phẩy hai cột giữa, chữ số đủ bốn cột.
    public static bool EssayDigitCell(int column, int row) => row switch
    {
        0 => column == 0,
        1 => column is 1 or 2,
        _ => column is >= 0 and < OmrDigitGrid.Columns
    };

    public static int EssayDigitBubbles { get; } =
        Enumerable.Range(0, OmrDigitGrid.Rows).Sum(row => Enumerable.Range(0, OmrDigitGrid.Columns).Count(column => EssayDigitCell(column, row)));

    // Phiếu có tự luận: phần II thêm hai câu hàng dưới, phần III thêm một câu bên phải.
    public static bool EssayCountAllowed(string kind, int count) => kind switch
    {
        BonDapAn => count is >= 1 and <= 8,
        DungSai => count is >= 1 and <= 6,
        DienSo => count is >= 1 and <= 3,
        _ => false
    };

    // Phiếu kiểm tra cho phần I tới ba hàng cột, mỗi hàng 40 câu.
    public static bool CheckCountAllowed(string kind, int count) => kind switch
    {
        BonDapAn => count is >= 1 and <= 120,
        DungSai => count is >= 1 and <= 8,
        _ => false
    };

    // Chuỗi xem trước: "10:BonDapAn,2:DungSai,2:DienSo".
    public static bool TryParseList(string? text, out List<(string Kind, int QuestionCount)> parts)
    {
        parts = [];
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var piece in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bits = piece.Split(':', StringSplitOptions.TrimEntries);
            if (bits.Length != 2 || !int.TryParse(bits[0], out var count) || !TryNormalize(bits[1], out var kind))
                return false;
            parts.Add((kind, count));
        }

        return parts.Count > 0;
    }
}
