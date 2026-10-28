namespace AIQuizHSG.Domain.Difficulty;

// Tên độ khó mục tiêu của cả đề. Khác với tên 5 mức của từng câu hỏi.
public static class ExamTargetDifficulty
{
    public static bool IsValid(byte level) => level is >= 1 and <= 5;

    public static string Name(byte level) => level switch
    {
        1 => "Rất dễ",
        2 => "Dễ",
        3 => "Trung bình",
        4 => "Khó",
        5 => "HSG chuyên sâu",
        _ => string.Empty
    };
}
