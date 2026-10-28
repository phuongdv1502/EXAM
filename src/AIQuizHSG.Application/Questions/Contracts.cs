using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Questions;

public sealed class QuestionFilter
{
    public long? SubjectId { get; set; }
    public long? TopicId { get; set; }
    public QuestionLevel? Level { get; set; }
    public byte? DifficultyLevelId { get; set; }
    public QuestionStatus? Status { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record SubjectResponse(long Id, string Code, string Name, string? Description, bool IsActive);

public sealed class SaveSubjectRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record TopicResponse(
    long Id,
    long SubjectId,
    long? ParentTopicId,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);

public sealed class SaveTopicRequest
{
    public long SubjectId { get; set; }
    public long? ParentTopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record OptionResponse(long Id, string Content, bool IsCorrect, int SortOrder);

public sealed class OptionInput
{
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public sealed record QuestionSummary(
    long Id,
    long TopicId,
    string TopicName,
    long SubjectId,
    string SubjectName,
    string Content,
    QuestionLevel Level,
    QuestionType Type,
    QuestionStatus Status,
    QuestionSource Source,
    DateTime UpdatedAtUtc,
    byte? DifficultyLevelId,
    string? DifficultyCode,
    string? DifficultyName);

public sealed record QuestionResponse(
    long Id,
    long TopicId,
    string TopicName,
    long SubjectId,
    string SubjectName,
    string Content,
    QuestionLevel Level,
    QuestionType Type,
    QuestionStatus Status,
    QuestionSource Source,
    string? Explanation,
    string? RejectionReason,
    long CreatedById,
    DateTime UpdatedAtUtc,
    byte? DifficultyLevelId,
    string? DifficultyCode,
    string? DifficultyName,
    IReadOnlyList<OptionResponse> Options);

public sealed class SaveQuestionRequest
{
    public long TopicId { get; set; }
    public string Content { get; set; } = string.Empty;
    public QuestionLevel Level { get; set; } = QuestionLevel.Medium;
    public byte? DifficultyLevelId { get; set; }
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public string? Explanation { get; set; }
    public List<OptionInput> Options { get; set; } = [];
}

public sealed class RejectQuestionRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed record QuestionPage(int Total, IReadOnlyList<QuestionSummary> Items);

public sealed record ImportError(int Row, string Message);

public sealed record ImportResult(int Imported, IReadOnlyList<ImportError> Errors);
