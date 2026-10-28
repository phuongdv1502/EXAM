using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Mappers;

// Gom chỗ gán câu hỏi và phương án, để service chỉ còn kiểm tra và lưu.
public static class QuestionMapper
{
    // Câu tạo tay vào nháp. Chuyên đề truyền vào đang được theo dõi nên gán navigation an toàn.
    public static Question CreateDraft(SaveQuestionRequest request, Topic topic, Actor actor, DateTime now) => new()
    {
        TopicId = request.TopicId,
        Topic = topic,
        Content = request.Content.Trim(),
        Level = request.Level,
        Type = request.Type,
        Status = QuestionStatus.Draft,
        Source = QuestionSource.Manual,
        Explanation = NormalizeOptional(request.Explanation),
        CreatedById = actor.UserId,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        Options = BuildOptions(request.Options)
    };

    // Câu nhập Excel vào thẳng chờ duyệt. Không gán Topic vì bản tra cứu không được theo dõi.
    public static Question CreateImported(ParsedQuestionRow row, long topicId, Actor actor, DateTime now) => new()
    {
        TopicId = topicId,
        Content = row.Content,
        Level = row.Level,
        Type = row.Type,
        Status = QuestionStatus.PendingReview,
        Source = QuestionSource.Manual,
        Explanation = row.Explanation,
        CreatedById = actor.UserId,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        Options = BuildOptions(row.Options)
    };

    // Gán lại nội dung. Không đụng trạng thái duyệt.
    public static void ApplyContent(Question question, SaveQuestionRequest request, Topic topic)
    {
        question.TopicId = request.TopicId;
        question.Topic = topic;
        question.Content = request.Content.Trim();
        question.Level = request.Level;
        question.Type = request.Type;
        question.Explanation = NormalizeOptional(request.Explanation);
        question.UpdatedAtUtc = DateTime.UtcNow;
    }

    // Sửa câu đã duyệt thì xóa người duyệt, service đổi trạng thái về chờ duyệt.
    public static void ClearReview(Question question)
    {
        question.ReviewedById = null;
        question.ReviewedAtUtc = null;
    }

    public static void MarkSubmitted(Question question)
    {
        question.Status = QuestionStatus.PendingReview;
        question.RejectionReason = null;
        ClearReview(question);
        question.UpdatedAtUtc = DateTime.UtcNow;
    }

    public static void MarkApproved(Question question, long reviewerId, DateTime now)
    {
        question.Status = QuestionStatus.Approved;
        question.RejectionReason = null;
        question.ReviewedById = reviewerId;
        question.ReviewedAtUtc = now;
        question.UpdatedAtUtc = now;
    }

    public static void MarkRejected(Question question, long reviewerId, string reason, DateTime now)
    {
        question.Status = QuestionStatus.Rejected;
        question.RejectionReason = reason.Trim();
        question.ReviewedById = reviewerId;
        question.ReviewedAtUtc = now;
        question.UpdatedAtUtc = now;
    }

    public static List<(string Content, bool IsCorrect)> OptionPairs(IEnumerable<QuestionOption> options) =>
        options.OrderBy(option => option.SortOrder).Select(option => (option.Content, option.IsCorrect)).ToList();

    public static List<QuestionOption> BuildOptions(IReadOnlyList<OptionInput> options)
    {
        var result = new List<QuestionOption>();
        for (var index = 0; index < options.Count; index++)
        {
            result.Add(new QuestionOption
            {
                Content = options[index].Content.Trim(),
                IsCorrect = options[index].IsCorrect,
                SortOrder = index + 1
            });
        }

        return result;
    }

    public static QuestionSummary ToSummary(Question question) => new(
        question.Id,
        question.TopicId,
        question.Topic.Name,
        question.Topic.SubjectId,
        question.Topic.Subject.Name,
        question.Content,
        question.Level,
        question.Type,
        question.Status,
        question.Source,
        question.UpdatedAtUtc,
        question.DifficultyLevelId,
        question.DifficultyLevel?.Code,
        question.DifficultyLevel?.Name);

    public static QuestionResponse ToResponse(Question question) => new(
        question.Id,
        question.TopicId,
        question.Topic.Name,
        question.Topic.SubjectId,
        question.Topic.Subject.Name,
        question.Content,
        question.Level,
        question.Type,
        question.Status,
        question.Source,
        question.Explanation,
        question.RejectionReason,
        question.CreatedById,
        question.UpdatedAtUtc,
        question.DifficultyLevelId,
        question.DifficultyLevel?.Code,
        question.DifficultyLevel?.Name,
        question.Options
            .OrderBy(option => option.SortOrder)
            .Select(option => new OptionResponse(option.Id, option.Content, option.IsCorrect, option.SortOrder))
            .ToArray());

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
