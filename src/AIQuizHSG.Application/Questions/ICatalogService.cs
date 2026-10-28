using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Questions;

public interface ICatalogService
{
    Task<IReadOnlyList<SubjectResponse>> ListSubjectsAsync(CancellationToken ct);
    Task<OperationResult<SubjectResponse>> CreateSubjectAsync(SaveSubjectRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<SubjectResponse>> UpdateSubjectAsync(long id, SaveSubjectRequest request, Actor actor, CancellationToken ct);
    Task<IReadOnlyList<TopicResponse>> ListTopicsAsync(long? subjectId, CancellationToken ct);
    Task<OperationResult<TopicResponse>> CreateTopicAsync(SaveTopicRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<TopicResponse>> UpdateTopicAsync(long id, SaveTopicRequest request, Actor actor, CancellationToken ct);
}
