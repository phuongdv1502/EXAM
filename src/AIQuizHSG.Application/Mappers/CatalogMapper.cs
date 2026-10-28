using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Mappers;

// Gom chỗ gán môn học và chuyên đề từ request, và chỗ trả response.
public static class CatalogMapper
{
    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static Subject CreateSubject(SaveSubjectRequest request) => new()
    {
        Code = NormalizeCode(request.Code),
        Name = request.Name.Trim(),
        Description = NormalizeOptional(request.Description),
        IsActive = request.IsActive
    };

    public static void ApplySubject(Subject subject, SaveSubjectRequest request)
    {
        subject.Code = NormalizeCode(request.Code);
        subject.Name = request.Name.Trim();
        subject.Description = NormalizeOptional(request.Description);
        subject.IsActive = request.IsActive;
    }

    public static Topic CreateTopic(SaveTopicRequest request) => new()
    {
        SubjectId = request.SubjectId,
        ParentTopicId = request.ParentTopicId,
        Name = request.Name.Trim(),
        Description = NormalizeOptional(request.Description),
        SortOrder = request.SortOrder,
        IsActive = request.IsActive
    };

    public static void ApplyTopic(Topic topic, SaveTopicRequest request)
    {
        topic.SubjectId = request.SubjectId;
        topic.ParentTopicId = request.ParentTopicId;
        topic.Name = request.Name.Trim();
        topic.Description = NormalizeOptional(request.Description);
        topic.SortOrder = request.SortOrder;
        topic.IsActive = request.IsActive;
    }

    public static SubjectResponse ToSubject(Subject subject) =>
        new(subject.Id, subject.Code, subject.Name, subject.Description, subject.IsActive);

    public static TopicResponse ToTopic(Topic topic) =>
        new(topic.Id, topic.SubjectId, topic.ParentTopicId, topic.Name, topic.Description, topic.SortOrder, topic.IsActive);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
