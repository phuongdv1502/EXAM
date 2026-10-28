using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Organization;

public sealed class SaveSchoolRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record SchoolResponse(
    long Id, string Code, string Name, string? Address, string? ProvinceCode, string? DistrictCode,
    string? Phone, string? Email, bool IsActive);

public sealed class SaveSchoolYearRequest
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public SchoolYearStatus Status { get; set; } = SchoolYearStatus.Planned;
}

public sealed record SchoolYearResponse(
    long Id, long SchoolId, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, SchoolYearStatus Status);

public sealed class SaveGradeLevelRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EducationLevel { get; set; } = "THCS";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record GradeLevelResponse(long Id, string Code, string Name, string EducationLevel, int SortOrder, bool IsActive);

public sealed class SaveClassroomRequest
{
    public long SchoolYearId { get; set; }
    public long GradeLevelId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? HomeroomTeacherUserId { get; set; }
    public int? Capacity { get; set; }
    public ClassroomStatus Status { get; set; } = ClassroomStatus.Active;
}

public sealed record ClassroomResponse(
    long Id, long SchoolId, long SchoolYearId, string SchoolYearName, long GradeLevelId, string GradeLevelName,
    string ClassCode, string Name, long? HomeroomTeacherUserId, string? HomeroomTeacherName, int? Capacity,
    int StudentCount, ClassroomStatus Status);

public sealed class SaveStudentProfileRequest
{
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public long? UserId { get; set; }
    public StudentProfileStatus Status { get; set; } = StudentProfileStatus.Active;
}

public sealed record StudentProfileResponse(
    long Id, long SchoolId, string StudentCode, string FullName, DateOnly? DateOfBirth, Gender? Gender,
    string? Email, string? Phone, string? Address, long? UserId, StudentProfileStatus Status,
    long? ClassroomId, string? ClassCode);

public sealed class EnrollStudentRequest
{
    public long StudentId { get; set; }
    public DateOnly? EnrollmentDate { get; set; }
    public string? Note { get; set; }
}

public sealed class TransferStudentRequest
{
    public long ClassroomId { get; set; }
    public DateOnly? TransferDate { get; set; }
    public string? Note { get; set; }
}

public sealed class AddSchoolMemberRequest
{
    public long UserId { get; set; }
    public SchoolRole SchoolRole { get; set; }
    public DateOnly? StartDate { get; set; }
}

public sealed record SchoolMemberResponse(long Id, long UserId, string FullName, string Username, SchoolRole SchoolRole, MembershipStatus Status);

public sealed class AssignTeacherRequest
{
    public long UserId { get; set; }
    public long SubjectId { get; set; }
}

public sealed record ClassTeacherResponse(long UserId, string FullName, long SubjectId, string SubjectName);

public sealed class AssignExamRequest
{
    public long? ClassroomId { get; set; }
    public long? StudentId { get; set; }
    public DateTime? DueAtUtc { get; set; }
}

public sealed record ExamAssignmentResponse(long Id, long ExamId, long? ClassroomId, long? StudentId, DateTime AssignedAtUtc, DateTime? DueAtUtc);

public sealed record StudentExamLine(long ExamId, string Title, string? AttemptStatus, decimal? Score, decimal? MaxScore);

public sealed record ClassroomExamResult(long StudentId, string StudentCode, string FullName, IReadOnlyList<StudentExamLine> Exams);

public sealed class ReplacePermissionsRequest
{
    public List<long> PermissionIds { get; set; } = [];
}

public sealed class ReplaceUserRolesRequest
{
    public List<long> RoleIds { get; set; } = [];
}

public sealed record PermissionResponse(long Id, string Code, string Name, string Module, string? Description);

public sealed record RoleResponse(long Id, string Code, string Name);
