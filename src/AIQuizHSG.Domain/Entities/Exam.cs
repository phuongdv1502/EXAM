using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Đề thi. Học sinh chỉ xem điểm khi PublishResults bật.
public class Exam
{
    public long Id { get; set; }
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
    public long? DifficultyProfileId { get; set; }
    public int? DifficultyProfileVersion { get; set; }
    public byte? TargetDifficultySnapshot { get; set; }
    public int? QuestionCountSnapshot { get; set; }
    public int? GenerationSeed { get; set; }
    public string? GenerationAlgorithmVersion { get; set; }
    public string? GenerationRequestKey { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public long CreatedById { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public Subject Subject { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ExamDifficultyProfile? DifficultyProfile { get; set; }
    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamParticipant> Participants { get; set; } = new List<ExamParticipant>();
}

// Bản chụp câu hỏi lúc đưa vào đề, để sửa ngân hàng sau này không đổi đề đã phát.
public class ExamQuestion
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long? SourceQuestionId { get; set; }
    public long? TopicId { get; set; }
    public string ContentSnapshot { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public QuestionLevel Level { get; set; }
    public byte? DifficultyLevelId { get; set; }
    public decimal Points { get; set; }
    public int SortOrder { get; set; }

    public Exam Exam { get; set; } = null!;
    public Question? SourceQuestion { get; set; }
    public ICollection<ExamOption> Options { get; set; } = new List<ExamOption>();
}

// Bản chụp phương án trong đề. IsCorrect chỉ dùng khi chấm ở máy chủ.
public class ExamOption
{
    public long Id { get; set; }
    public long ExamQuestionId { get; set; }
    public long? SourceOptionId { get; set; }
    public string ContentSnapshot { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }

    public ExamQuestion ExamQuestion { get; set; } = null!;
    public QuestionOption? SourceOption { get; set; }
}

// Học sinh được phép vào một đề.
public class ExamParticipant
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long UserId { get; set; }

    public Exam Exam { get; set; } = null!;
    public User User { get; set; } = null!;
}
