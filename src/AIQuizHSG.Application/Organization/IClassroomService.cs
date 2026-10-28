using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Organization;

public interface IClassroomService
{
    Task<OperationResult<IReadOnlyList<ClassroomResponse>>> ListAsync(long schoolId, long? schoolYearId, Actor actor, CancellationToken ct);
    Task<OperationResult<ClassroomResponse>> CreateAsync(long schoolId, SaveClassroomRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ClassroomResponse>> UpdateAsync(long id, SaveClassroomRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ClassTeacherResponse>> AssignTeacherAsync(long classroomId, AssignTeacherRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<StudentProfileResponse>>> ListStudentsAsync(long classroomId, Actor actor, CancellationToken ct);
}
