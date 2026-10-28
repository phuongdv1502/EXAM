using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class SchoolRepository(AiQuizDbContext db) : ISchoolRepository
{
    public Task<List<School>> ListAsync(CancellationToken ct) =>
        db.Schools.AsNoTracking().OrderBy(school => school.Name).ToListAsync(ct);

    public Task<School?> FindAsync(long id, CancellationToken ct) =>
        db.Schools.FirstOrDefaultAsync(school => school.Id == id, ct);

    public Task<School?> FindByCodeAsync(string code, CancellationToken ct) =>
        db.Schools.AsNoTracking().FirstOrDefaultAsync(school => school.Code == code, ct);

    public async Task AddAsync(School school, CancellationToken ct) =>
        await db.Schools.AddAsync(school, ct);

    public Task<List<SchoolYear>> ListYearsAsync(long schoolId, CancellationToken ct) =>
        db.SchoolYears.AsNoTracking()
            .Where(year => year.SchoolId == schoolId)
            .OrderByDescending(year => year.StartDate)
            .ToListAsync(ct);

    public Task<SchoolYear?> FindYearAsync(long id, CancellationToken ct) =>
        db.SchoolYears.Include(year => year.School).FirstOrDefaultAsync(year => year.Id == id, ct);

    public Task<SchoolYear?> FindYearByNameAsync(long schoolId, string name, CancellationToken ct) =>
        db.SchoolYears.AsNoTracking()
            .FirstOrDefaultAsync(year => year.SchoolId == schoolId && year.Name == name, ct);

    public async Task AddYearAsync(SchoolYear year, CancellationToken ct) =>
        await db.SchoolYears.AddAsync(year, ct);

    // Bỏ cờ hiện hành của các năm khác trong cùng trường trước khi đánh dấu năm mới.
    public async Task ClearCurrentAsync(long schoolId, long? exceptYearId, CancellationToken ct)
    {
        var currents = await db.SchoolYears
            .Where(year => year.SchoolId == schoolId && year.IsCurrent && year.Id != exceptYearId)
            .ToListAsync(ct);
        foreach (var year in currents)
            year.IsCurrent = false;
    }

    public Task<bool> HasActiveMembershipAsync(long userId, long schoolId, CancellationToken ct) =>
        db.SchoolMemberships.AnyAsync(item =>
            item.UserId == userId && item.SchoolId == schoolId && item.Status == MembershipStatus.Active, ct);

    public Task<bool> HasActiveRoleAsync(long userId, long schoolId, SchoolRole role, CancellationToken ct) =>
        db.SchoolMemberships.AnyAsync(item =>
            item.UserId == userId
            && item.SchoolId == schoolId
            && item.SchoolRole == role
            && item.Status == MembershipStatus.Active, ct);

    public Task<SchoolMembership?> FindMembershipAsync(long schoolId, long userId, SchoolRole role, CancellationToken ct) =>
        db.SchoolMemberships.FirstOrDefaultAsync(item =>
            item.SchoolId == schoolId && item.UserId == userId && item.SchoolRole == role, ct);

    public Task<List<SchoolMembership>> ListMembershipsAsync(long schoolId, CancellationToken ct) =>
        db.SchoolMemberships.AsNoTracking()
            .Include(item => item.User)
            .Where(item => item.SchoolId == schoolId)
            .OrderBy(item => item.User.FullName)
            .ToListAsync(ct);

    public async Task AddMembershipAsync(SchoolMembership membership, CancellationToken ct) =>
        await db.SchoolMemberships.AddAsync(membership, ct);
}

public sealed class GradeLevelRepository(AiQuizDbContext db) : IGradeLevelRepository
{
    public Task<bool> AnyAsync(CancellationToken ct) => db.GradeLevels.AnyAsync(ct);

    public Task<List<GradeLevel>> ListAsync(CancellationToken ct) =>
        db.GradeLevels.AsNoTracking().OrderBy(level => level.SortOrder).ToListAsync(ct);

    public Task<GradeLevel?> FindAsync(long id, CancellationToken ct) =>
        db.GradeLevels.FirstOrDefaultAsync(level => level.Id == id, ct);

    public Task<GradeLevel?> FindByCodeAsync(string code, CancellationToken ct) =>
        db.GradeLevels.AsNoTracking().FirstOrDefaultAsync(level => level.Code == code, ct);

    public async Task AddAsync(GradeLevel level, CancellationToken ct) =>
        await db.GradeLevels.AddAsync(level, ct);

    public async Task AddRangeAsync(IEnumerable<GradeLevel> levels, CancellationToken ct) =>
        await db.GradeLevels.AddRangeAsync(levels, ct);
}

public sealed class ClassroomRepository(AiQuizDbContext db) : IClassroomRepository
{
    public Task<List<Classroom>> ListAsync(long schoolId, long? schoolYearId, CancellationToken ct) =>
        Query().Where(room => room.SchoolId == schoolId && (schoolYearId == null || room.SchoolYearId == schoolYearId))
            .OrderBy(room => room.ClassCode)
            .ToListAsync(ct);

    public Task<Classroom?> FindAsync(long id, CancellationToken ct) =>
        db.Classrooms
            .Include(room => room.SchoolYear)
            .Include(room => room.GradeLevel)
            .Include(room => room.HomeroomTeacher)
            .FirstOrDefaultAsync(room => room.Id == id, ct);

    public Task<Classroom?> FindByCodeAsync(long schoolYearId, string classCode, CancellationToken ct) =>
        db.Classrooms.AsNoTracking()
            .FirstOrDefaultAsync(room => room.SchoolYearId == schoolYearId && room.ClassCode == classCode, ct);

    public async Task AddAsync(Classroom classroom, CancellationToken ct) =>
        await db.Classrooms.AddAsync(classroom, ct);

    public Task<int> CountActiveStudentsAsync(long classroomId, CancellationToken ct) =>
        db.StudentEnrollments.CountAsync(item =>
            item.ClassroomId == classroomId && item.Status == EnrollmentStatus.Active && item.LeaveDate == null, ct);

    // Chủ nhiệm hoặc giáo viên bộ môn đều được coi là phụ trách lớp.
    public async Task<bool> TeachesAsync(long classroomId, long userId, CancellationToken ct)
    {
        if (await db.Classrooms.AnyAsync(room => room.Id == classroomId && room.HomeroomTeacherUserId == userId, ct))
            return true;
        return await db.ClassMemberships.AnyAsync(item => item.ClassroomId == classroomId && item.UserId == userId, ct);
    }

    public Task<ClassMembership?> FindSubjectTeacherAsync(long classroomId, long userId, long subjectId, CancellationToken ct) =>
        db.ClassMemberships.AsNoTracking().FirstOrDefaultAsync(item =>
            item.ClassroomId == classroomId && item.UserId == userId && item.SubjectId == subjectId, ct);

    public Task<List<ClassMembership>> ListTeachersAsync(long classroomId, CancellationToken ct) =>
        db.ClassMemberships.AsNoTracking()
            .Include(item => item.User)
            .Include(item => item.Subject)
            .Where(item => item.ClassroomId == classroomId)
            .ToListAsync(ct);

    public async Task AddTeacherAsync(ClassMembership membership, CancellationToken ct) =>
        await db.ClassMemberships.AddAsync(membership, ct);

    private IQueryable<Classroom> Query() =>
        db.Classrooms.AsNoTracking().Include(room => room.GradeLevel).Include(room => room.SchoolYear);
}

public sealed class StudentRecordRepository(AiQuizDbContext db) : IStudentRecordRepository
{
    public Task<List<StudentProfile>> SearchAsync(long schoolId, string? keyword, CancellationToken ct)
    {
        var query = db.Students.AsNoTracking().Where(student => student.SchoolId == schoolId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var text = keyword.Trim();
            query = query.Where(student => student.FullName.Contains(text) || student.StudentCode.Contains(text));
        }

        return query.OrderBy(student => student.FullName).ToListAsync(ct);
    }

    public Task<StudentProfile?> FindAsync(long id, CancellationToken ct) =>
        db.Students
            .Include(student => student.Enrollments)
            .ThenInclude(enrollment => enrollment.Classroom)
            .FirstOrDefaultAsync(student => student.Id == id, ct);

    public Task<StudentProfile?> FindByCodeAsync(long schoolId, string code, CancellationToken ct) =>
        db.Students.AsNoTracking()
            .FirstOrDefaultAsync(student => student.SchoolId == schoolId && student.StudentCode == code, ct);

    public Task<StudentProfile?> FindByUserAsync(long userId, CancellationToken ct) =>
        db.Students.AsNoTracking().FirstOrDefaultAsync(student => student.UserId == userId, ct);

    public async Task AddAsync(StudentProfile student, CancellationToken ct) =>
        await db.Students.AddAsync(student, ct);

    public Task<StudentEnrollment?> FindActiveEnrollmentAsync(long studentId, CancellationToken ct) =>
        db.StudentEnrollments
            .Include(item => item.Classroom)
            .FirstOrDefaultAsync(item =>
                item.StudentId == studentId && item.Status == EnrollmentStatus.Active && item.LeaveDate == null, ct);

    public Task<List<StudentProfile>> ListActiveInClassroomAsync(long classroomId, CancellationToken ct) =>
        db.StudentEnrollments.AsNoTracking()
            .Where(item => item.ClassroomId == classroomId && item.Status == EnrollmentStatus.Active && item.LeaveDate == null)
            .Select(item => item.Student)
            .OrderBy(student => student.FullName)
            .ToListAsync(ct);

    public async Task AddEnrollmentAsync(StudentEnrollment enrollment, CancellationToken ct) =>
        await db.StudentEnrollments.AddAsync(enrollment, ct);
}

public sealed class PermissionRepository(AiQuizDbContext db) : IPermissionRepository
{
    public Task<bool> AnyAsync(CancellationToken ct) => db.Permissions.AnyAsync(ct);

    public Task<bool> AnyRoleLinkAsync(CancellationToken ct) => db.RolePermissions.AnyAsync(ct);

    public Task<List<Permission>> ListAsync(string? module, CancellationToken ct) =>
        db.Permissions.AsNoTracking()
            .Where(item => module == null || item.Module == module)
            .OrderBy(item => item.Module)
            .ThenBy(item => item.Code)
            .ToListAsync(ct);

    public Task<List<Permission>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) =>
        db.Permissions.AsNoTracking().Where(item => ids.Contains(item.Id) && item.IsActive).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken ct) =>
        await db.Permissions.AddRangeAsync(permissions, ct);

    // Quyền hiệu lực = quyền vai trò, cộng Allow và trừ Deny gắn riêng cho người đó.
    public async Task<List<string>> ListCodesForUserAsync(long userId, CancellationToken ct)
    {
        var codes = await db.RolePermissions.AsNoTracking()
            .Where(link => link.Role.UserRoles.Any(userRole => userRole.UserId == userId) && link.Permission.IsActive)
            .Select(link => link.Permission.Code)
            .Distinct()
            .ToListAsync(ct);
        var grants = await db.UserPermissions.AsNoTracking()
            .Where(item => item.UserId == userId && item.Permission.IsActive)
            .Select(item => new { item.Permission.Code, item.Effect })
            .ToListAsync(ct);
        var effective = codes.ToHashSet();
        foreach (var grant in grants)
        {
            if (grant.Effect == PermissionEffect.Deny)
                effective.Remove(grant.Code);
            else
                effective.Add(grant.Code);
        }

        return effective.OrderBy(code => code).ToList();
    }

    public Task<List<Permission>> ListForRoleAsync(long roleId, CancellationToken ct) =>
        db.RolePermissions.AsNoTracking()
            .Where(link => link.RoleId == roleId)
            .Select(link => link.Permission)
            .OrderBy(item => item.Module)
            .ThenBy(item => item.Code)
            .ToListAsync(ct);

    // Thay cả tập quyền của vai trò trong một lần lưu.
    public async Task ReplaceRoleAsync(long roleId, IReadOnlyCollection<long> permissionIds, long? actorId, DateTime now, CancellationToken ct)
    {
        var existing = await db.RolePermissions.Where(link => link.RoleId == roleId).ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing);
        foreach (var permissionId in permissionIds.Distinct())
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAtUtc = now,
                CreatedByUserId = actorId
            });
        }
    }

    public async Task AddRoleLinksAsync(IEnumerable<RolePermission> links, CancellationToken ct) =>
        await db.RolePermissions.AddRangeAsync(links, ct);
}

public sealed class ExamAssignmentRepository(AiQuizDbContext db) : IExamAssignmentRepository
{
    public async Task AddAsync(ExamAssignment assignment, CancellationToken ct) =>
        await db.ExamAssignments.AddAsync(assignment, ct);

    public Task<ExamAssignment?> FindActiveAsync(long examId, long? classroomId, long? studentId, CancellationToken ct) =>
        db.ExamAssignments.AsNoTracking().FirstOrDefaultAsync(item =>
            item.ExamId == examId
            && item.ClassroomId == classroomId
            && item.StudentId == studentId
            && item.Status == AssignmentStatus.Active, ct);

    public Task<List<ExamAssignment>> ListByClassroomAsync(long classroomId, CancellationToken ct) =>
        db.ExamAssignments.AsNoTracking()
            .Include(item => item.Exam)
            .Where(item => item.ClassroomId == classroomId && item.Status == AssignmentStatus.Active)
            .ToListAsync(ct);

    // Không có giao đề thì giữ luật danh sách thí sinh cũ. Có giao thì chỉ hồ sơ được giao mới vào.
    public async Task<bool> AllowsAsync(long examId, long userId, CancellationToken ct)
    {
        var targets = await db.ExamAssignments.AsNoTracking()
            .Where(item => item.ExamId == examId && item.Status == AssignmentStatus.Active)
            .Select(item => new { item.ClassroomId, item.StudentId })
            .ToListAsync(ct);
        if (targets.Count == 0)
            return true;

        var studentId = await db.Students.AsNoTracking()
            .Where(student => student.UserId == userId)
            .Select(student => (long?)student.Id)
            .FirstOrDefaultAsync(ct);
        if (studentId is null)
            return false;
        if (targets.Any(item => item.StudentId == studentId))
            return true;

        var classIds = targets.Where(item => item.ClassroomId != null).Select(item => item.ClassroomId!.Value).ToArray();
        if (classIds.Length == 0)
            return false;

        return await db.StudentEnrollments.AnyAsync(item =>
            item.StudentId == studentId
            && item.Status == EnrollmentStatus.Active
            && item.LeaveDate == null
            && classIds.Contains(item.ClassroomId), ct);
    }
}
