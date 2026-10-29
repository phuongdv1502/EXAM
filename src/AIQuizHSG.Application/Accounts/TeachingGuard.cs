using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Accounts;

// Quản trị tạo mọi môn. Giáo viên chỉ tạo nội dung thuộc môn được phân dạy.
public static class TeachingGuard
{
    public static async Task<string?> DenyAsync(IUnitOfWork unitOfWork, long userId, long subjectId, CancellationToken ct)
    {
        var user = await unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user?.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin) == true)
            return null;
        if (await unitOfWork.TeacherSubjects.AllowsAsync(userId, subjectId, ct))
            return null;
        return Messages.SubjectNotTaught;
    }
}
