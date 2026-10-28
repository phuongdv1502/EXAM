using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Difficulty;

public sealed class SaveExamProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public byte TargetDifficulty { get; set; }
    public int BaseQuestionCount { get; set; }
    public string ScalingMode { get; set; } = "PROPORTIONAL";
    public List<ProfileLevelInput> DifficultyDistribution { get; set; } = [];
}

public sealed class ProfileLevelInput
{
    public byte Level { get; set; }
    public int Count { get; set; }
}

public sealed record ProfileLevelResponse(byte Level, string Name, int Count, decimal Ratio);

public sealed record ExamProfileResponse(
    long Id,
    string Name,
    byte TargetDifficulty,
    string TargetDifficultyName,
    int BaseQuestionCount,
    string ScalingMode,
    bool IsSystemDefault,
    int Version,
    ProfileStatus Status,
    IReadOnlyList<ProfileLevelResponse> Levels);

public sealed class PreviewProfileRequest
{
    public int QuestionCount { get; set; }
}

public sealed record DistributionLine(byte Level, int Count);

public sealed record ProfilePreviewResponse(
    byte TargetDifficulty,
    int QuestionCount,
    string ScalingMode,
    IReadOnlyList<DistributionLine> Distribution,
    bool IsValid,
    string? Message);

public sealed class AvailabilityRequest
{
    public long SubjectId { get; set; }
    public long ProfileId { get; set; }
    public int QuestionCount { get; set; }
    public List<long> TopicIds { get; set; } = [];
    public QuestionType? QuestionType { get; set; }
}

public sealed record ShortageLine(byte Level, int Required, int Available, int Missing);

public sealed record AvailabilityError(string Code, string Message);

public sealed record AvailabilityResponse(
    bool CanGenerate,
    int RequiredQuestionCount,
    int AvailableQuestionCount,
    IReadOnlyList<ShortageLine> Shortages,
    IReadOnlyList<AvailabilityError> Errors);

public sealed class GenerateExamRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long SubjectId { get; set; }
    public long ProfileId { get; set; }
    public int QuestionCount { get; set; }
    public List<long> TopicIds { get; set; } = [];
    public QuestionType? QuestionType { get; set; }
    public decimal DefaultPoints { get; set; } = 1;
    public int DurationMinutes { get; set; }
    public DateTime? OpensAtUtc { get; set; }
    public DateTime? ClosesAtUtc { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleOptions { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public bool PublishResults { get; set; }
    public List<long> ParticipantUserIds { get; set; } = [];
    public string? IdempotencyKey { get; set; }
}

public sealed record ExamValidationResponse(bool IsValid, IReadOnlyList<AvailabilityError> Errors);

public static class DifficultyCodes
{
    public const string InsufficientQuestions = "INSUFFICIENT_QUESTIONS";
    public const string FixedCountMismatch = "FIXED_COUNT_MISMATCH";
    public const string InvalidProfile = "INVALID_PROFILE";
    public const string DistributionMismatch = "DISTRIBUTION_MISMATCH";
}
