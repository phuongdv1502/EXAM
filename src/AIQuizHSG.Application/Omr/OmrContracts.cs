namespace AIQuizHSG.Application.Omr;

public sealed class SaveOmrTemplateRequest
{
    public string Code { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public int QuestionCount { get; set; }
    public int ChoiceCount { get; set; } = 4;
    public bool IsActive { get; set; }
    public string? ConfigJson { get; set; }
}

public sealed class CreateOmrBatchRequest
{
    public long ExamId { get; set; }
    public long TemplateId { get; set; }
    public string SourceType { get; set; } = "Scanner";
}

public sealed class ReviewOmrSheetRequest
{
    public string? StudentCode { get; set; }
    public string? ExamCode { get; set; }
    public string? ReviewNote { get; set; }
    public List<ReviewOmrAnswer> Answers { get; set; } = [];
}

public sealed class ReviewOmrAnswer
{
    public long ExamQuestionId { get; set; }
    public List<long> SelectedOptionIds { get; set; } = [];
}

public sealed record OmrTemplateSummary(
    long Id,
    string Code,
    int Version,
    int QuestionCount,
    int ChoiceCount,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record OmrBatchSummary(
    long Id,
    long ExamId,
    string ExamTitle,
    long TemplateId,
    string TemplateCode,
    string ExamCode,
    string SourceType,
    string Status,
    int TotalSheets,
    int ProcessedSheets,
    int FailedSheets,
    int NeedsReviewSheets,
    DateTime CreatedAtUtc);

public sealed record OmrSheetSummary(
    long Id,
    int PageNumber,
    string FileName,
    string? StudentCode,
    string? ExamCode,
    string Status,
    string ReviewStatus,
    decimal Confidence,
    string? FailureReason,
    long? ExamAttemptId);

public sealed record OmrSheetPage(IReadOnlyList<OmrSheetSummary> Items, int Total);

public sealed record UploadedOmrSheet(long SheetId, string FileName, int PageNumber, bool Duplicate, string Status);

public sealed record UploadOmrSheetsResult(long BatchId, IReadOnlyList<UploadedOmrSheet> Sheets);

public sealed record OmrChoiceDetail(long Id, string Content, int SortOrder);

public sealed record OmrAnswerDetail(
    int QuestionNumber,
    long? ExamQuestionId,
    string? Content,
    string? QuestionType,
    string RecognitionStatus,
    decimal Confidence,
    long? RecognizedOptionId,
    IReadOnlyList<long> ConfirmedOptionIds,
    IReadOnlyList<OmrChoiceDetail> Choices);

public sealed record OmrSheetDetail(
    long Id,
    long BatchId,
    string Status,
    string ReviewStatus,
    decimal Confidence,
    string? StudentCode,
    string? ExamCode,
    string ExpectedExamCode,
    string? FailureReason,
    string? ReviewNote,
    IReadOnlyList<OmrAnswerDetail> Answers,
    string BatchStatus);

public sealed record OmrResultRow(
    long SheetId,
    string? StudentCode,
    string? StudentName,
    string? ExamCode,
    int Correct,
    int Wrong,
    int Blank,
    decimal? Score,
    decimal? MaxScore,
    string Status);

public sealed record OmrStoredFile(string RelativePath, string Sha256, string ContentType);

public sealed record OmrPageImage(int PageNumber, string ContentType, byte[] Content, string? Error);

public sealed class OmrBubbleReading
{
    public int QuestionNumber { get; init; }
    public int ChoiceIndex { get; init; }
    public double Ratio { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class OmrRecognitionResult
{
    public string? StudentCode { get; init; }
    public string? ExamCode { get; init; }
    public bool QrFound { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<OmrBubbleReading> Bubbles { get; init; } = [];
}
