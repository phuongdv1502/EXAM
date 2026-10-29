using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Một đợt nhập phiếu của một đề. Mã đề lấy theo đề đã chọn, không do người dùng nhập.
public class OmrImportBatch
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long TemplateId { get; set; }
    public long CreatedById { get; set; }
    public string ExamCode { get; set; } = string.Empty;
    public OmrSourceType SourceType { get; set; } = OmrSourceType.Scanner;
    public OmrBatchStatus Status { get; set; } = OmrBatchStatus.Draft;
    public int TotalSheets { get; set; }
    public int ProcessedSheets { get; set; }
    public int FailedSheets { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public long? ApprovedById { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public long? PublishedById { get; set; }

    public Exam Exam { get; set; } = null!;
    public OmrTemplate Template { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<OmrScannedSheet> Sheets { get; set; } = new List<OmrScannedSheet>();
}
