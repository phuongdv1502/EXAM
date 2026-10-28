using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Một câu trên phiếu. Giữ cả kết quả máy và kết quả người đã xác nhận.
public class OmrRecognizedAnswer
{
    public long Id { get; set; }
    public long SheetId { get; set; }
    public int QuestionNumber { get; set; }
    public long? ExamQuestionId { get; set; }
    public long? RecognizedOptionId { get; set; }
    public string DetectedMarksJson { get; set; } = "[]";
    public decimal Confidence { get; set; }
    public OmrMarkStatus RecognitionStatus { get; set; } = OmrMarkStatus.Blank;
    public string? ConfirmedMarksJson { get; set; }
    public long? ConfirmedById { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }

    public OmrScannedSheet Sheet { get; set; } = null!;
    public ExamQuestion? ExamQuestion { get; set; }
    public ExamOption? RecognizedOption { get; set; }
}
