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
