using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Questions;

namespace AIQuizHSG.Application.Organization;

public interface IStudentRecordService
{
    Task<OperationResult<IReadOnlyList<StudentProfileResponse>>> ListAsync(long schoolId, string? keyword, Actor actor, CancellationToken ct);
    Task<OperationResult<StudentProfileResponse>> CreateAsync(long schoolId, SaveStudentProfileRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<StudentProfileResponse>> UpdateAsync(long id, SaveStudentProfileRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<StudentProfileResponse>> EnrollAsync(long classroomId, EnrollStudentRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<StudentProfileResponse>> TransferAsync(long studentId, TransferStudentRequest request, Actor actor, CancellationToken ct);
    byte[] CreateImportTemplate();
    Task<OperationResult<ImportResult>> ImportAsync(long schoolId, long schoolYearId, Stream stream, Actor actor, CancellationToken ct);
}
