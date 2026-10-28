using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Một trang phiếu. Ảnh gốc nằm trên đĩa; bảng chỉ giữ đường dẫn và mã kiểm tra.
public class OmrScannedSheet
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public int PageNumber { get; set; } = 1;
    public string OriginalFileName { get; set; } = string.Empty;
    public string OriginalFilePath { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? RecognizedStudentCode { get; set; }
    public string? RecognizedExamCode { get; set; }
    public string? ConfirmedStudentCode { get; set; }
    public string? ConfirmedExamCode { get; set; }
    public long? ExamAttemptId { get; set; }
    public OmrSheetStatus Status { get; set; } = OmrSheetStatus.Uploaded;
    public OmrReviewStatus ReviewStatus { get; set; } = OmrReviewStatus.Pending;
    public decimal Confidence { get; set; }
    public string? FailureReason { get; set; }
    public string? ReviewNote { get; set; }
    public long? ReviewedById { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public OmrImportBatch Batch { get; set; } = null!;
    public ExamAttempt? ExamAttempt { get; set; }
    public User? ReviewedBy { get; set; }
    public ICollection<OmrRecognizedAnswer> Answers { get; set; } = new List<OmrRecognizedAnswer>();
}
