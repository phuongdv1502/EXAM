namespace AIQuizHSG.Domain.Entities;

// Một mã đề đảo của đề thi. Thứ tự câu và phương án khóa trong JSON để in và chấm phiếu.
public class ExamVariant
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string QuestionOrderJson { get; set; } = "[]";
    public string OptionOrderJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }

    public Exam Exam { get; set; } = null!;
}
