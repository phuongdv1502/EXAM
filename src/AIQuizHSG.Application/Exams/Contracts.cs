using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Exams;

public sealed class ExamQuestionInput
{
    public long QuestionId { get; set; }
    public decimal Points { get; set; }
}

public sealed class SaveExamRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long SubjectId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public bool PublishResults { get; set; }
    public List<ExamQuestionInput> Questions { get; set; } = [];
    public List<long> ParticipantUserIds { get; set; } = [];
}

public sealed record ExamSummary(
    long Id,
    string Title,
    long SubjectId,
    string SubjectName,
    int DurationMinutes,
    ExamStatus Status,
    int QuestionCount,
    int ParticipantCount,
    bool PublishResults,
    DateTime? OpensAtUtc,
    DateTime? ClosesAtUtc);

public sealed record ExamOptionResponse(long Id, string Content, bool IsCorrect, int SortOrder);

public sealed record ExamQuestionResponse(
    long Id,
    long? SourceQuestionId,
    long? TopicId,
    string Content,
    QuestionType Type,
    QuestionLevel Level,
    decimal Points,
    int SortOrder,
    byte? DifficultyLevelId,
    IReadOnlyList<ExamOptionResponse> Options);

public sealed record ExamParticipantResponse(long UserId, string FullName, string Username);

public sealed record ExamResponse(
    long Id,
    string Title,
    string? Description,
    long SubjectId,
    string SubjectName,
    int DurationMinutes,
    DateTime? OpensAtUtc,
    DateTime? ClosesAtUtc,
    bool ShuffleQuestions,
    bool ShuffleOptions,
    int MaxAttempts,
    bool PublishResults,
    ExamStatus Status,
    DateTime? PublishedAtUtc,
    long? DifficultyProfileId,
    int? DifficultyProfileVersion,
    byte? TargetDifficultySnapshot,
    int? QuestionCountSnapshot,
    IReadOnlyList<ExamQuestionResponse> Questions,
    IReadOnlyList<ExamParticipantResponse> Participants);

public sealed class SaveAnswerRequest
{
    public List<long> OptionIds { get; set; } = [];
}

// Đề học sinh đang làm. Không có cờ đáp án đúng.
public sealed record PaperOption(long Id, string Content);

public sealed record PaperQuestion(
    long ExamQuestionId,
    string Content,
    QuestionType Type,
    QuestionLevel Level,
    decimal Points,
    IReadOnlyList<PaperOption> Options,
    IReadOnlyList<long> SelectedOptionIds);

public sealed record AttemptPaper(
    long Id,
    long ExamId,
    string Title,
    int AttemptNumber,
    AttemptStatus Status,
    DateTime StartedAtUtc,
    DateTime DueAtUtc,
    DateTime ServerNowUtc,
    IReadOnlyList<PaperQuestion> Questions);

public sealed record AttemptSummary(
    long Id,
    long ExamId,
    string ExamTitle,
    int AttemptNumber,
    AttemptStatus Status,
    DateTime StartedAtUtc,
    DateTime DueAtUtc,
    DateTime? SubmittedAtUtc);
