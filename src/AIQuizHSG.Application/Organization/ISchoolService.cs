using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Organization;

public interface ISchoolService
{
    Task<OperationResult<IReadOnlyList<SchoolResponse>>> ListAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<SchoolResponse>> GetAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<SchoolResponse>> CreateAsync(SaveSchoolRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<SchoolResponse>> UpdateAsync(long id, SaveSchoolRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<SchoolYearResponse>>> ListYearsAsync(long schoolId, Actor actor, CancellationToken ct);
    Task<OperationResult<SchoolYearResponse>> CreateYearAsync(long schoolId, SaveSchoolYearRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<SchoolYearResponse>> UpdateYearAsync(long id, SaveSchoolYearRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<GradeLevelResponse>>> ListGradesAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<GradeLevelResponse>> CreateGradeAsync(SaveGradeLevelRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<SchoolMemberResponse>> AddMemberAsync(long schoolId, AddSchoolMemberRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<SchoolMemberResponse>>> ListMembersAsync(long schoolId, Actor actor, CancellationToken ct);
}
