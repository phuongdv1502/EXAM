using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Questions;

namespace AIQuizHSG.Application.Accounts;

public interface ITeachingSubjectService
{
    Task<IReadOnlyList<SubjectResponse>> ListMineAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<SubjectResponse>>> ListCatalogAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<SubjectResponse>>> ListForUserAsync(long userId, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<SubjectResponse>>> ReplaceAsync(long userId, IReadOnlyCollection<long> subjectIds, Actor actor, CancellationToken ct);
}
