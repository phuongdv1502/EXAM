using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Organization;

public interface ISchoolAccess
{
    Task<string?> DenyAsync(long userId, string permission, long? schoolId, CancellationToken ct);
    Task<bool> IsWideAsync(long userId, long schoolId, CancellationToken ct);
    Task<string?> DenyClassroomAsync(long userId, Classroom classroom, string permission, CancellationToken ct);
}

// Quyền chức năng và phạm vi trường/lớp phải cùng đúng. Quản trị hệ thống đi qua mọi phạm vi.
public sealed partial class SchoolAccess : BaseService, ISchoolAccess
{
    public SchoolAccess(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public async Task<string?> DenyAsync(long userId, string permission, long? schoolId, CancellationToken ct)
    {
        var user = await unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user is null || user.Status != UserStatus.Active)
            return Messages.AccountInactive;

        var admin = IsAdmin(user);
        if (!admin)
        {
            var codes = await unitOfWork.Permissions.ListCodesForUserAsync(userId, ct);
            if (!codes.Contains(permission))
                return Messages.PermissionDenied;
        }

        if (schoolId is null || admin)
            return null;

        if (!await unitOfWork.Schools.HasActiveMembershipAsync(userId, schoolId.Value, ct))
            return Messages.SchoolScopeDenied;

        return null;
    }

    public async Task<bool> IsWideAsync(long userId, long schoolId, CancellationToken ct)
    {
        var user = await unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user is null)
            return false;
        if (IsAdmin(user))
            return true;
        return await unitOfWork.Schools.HasActiveRoleAsync(userId, schoolId, SchoolRole.SchoolAdmin, ct);
    }

    // Giáo viên chỉ vào lớp mình chủ nhiệm hoặc được phân công. Quản trị trường vào mọi lớp của trường.
    public async Task<string?> DenyClassroomAsync(long userId, Classroom classroom, string permission, CancellationToken ct)
    {
        if (await DenyAsync(userId, permission, classroom.SchoolId, ct) is { } denied)
            return denied;
        if (await IsWideAsync(userId, classroom.SchoolId, ct))
            return null;
        if (await unitOfWork.Classrooms.TeachesAsync(classroom.Id, userId, ct))
            return null;
        return Messages.ClassScopeDenied;
    }

    private static bool IsAdmin(User user) =>
        user.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin);
}
