using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Ai;

public sealed class GenerateAiQuestionsRequest
{
    public long TopicId { get; set; }
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public QuestionLevel Level { get; set; } = QuestionLevel.Medium;
    public int QuestionCount { get; set; } = 3;
    public string? Hint { get; set; }
}

public sealed record GenerateAiQuestionsResult(
    long RequestId,
    int Created,
    IReadOnlyList<long> QuestionIds,
    IReadOnlyList<string> Errors);
