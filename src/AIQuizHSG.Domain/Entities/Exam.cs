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
