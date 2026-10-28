using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Organization;

public interface ISchoolAccess
{
    Task<string?> DenyAsync(long userId, string permission, long? schoolId, CancellationToken ct);
    Task<bool> IsWideAsync(long userId, long schoolId, CancellationToken ct);
    Task<string?> DenyClassroomAsync(long userId, Classroom classroom, string permission, CancellationToken ct);
}
