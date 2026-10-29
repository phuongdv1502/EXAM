namespace AIQuizHSG.Application.Omr;

// Mã kiểu phiếu. Bản vẽ và ảnh in phải dùng cùng một mã.
public static class OmrFormStyles
{
    public const string Khung = "Khung";
    public const string MotCot = "MotCot";
    public const string Luoi = "Luoi";

    public static bool TryNormalize(string? style, out string code)
    {
        code = style?.Trim() ?? string.Empty;
        if (code.Length == 0)
        {
            code = Khung;
            return true;
        }

        if (code is Khung or MotCot or Luoi)
            return true;

        code = string.Empty;
        return false;
    }
}
