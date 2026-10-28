using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Organization;

internal static class OrganizationMapper
{
    public static string Code(string value) => value.Trim().ToUpperInvariant();

    public static SchoolResponse ToSchool(School school) => new(
        school.Id, school.Code, school.Name, school.Address, school.ProvinceCode, school.DistrictCode,
        school.Phone, school.Email, school.IsActive);

    public static SchoolYearResponse ToYear(SchoolYear year) => new(
        year.Id, year.SchoolId, year.Name, year.StartDate, year.EndDate, year.IsCurrent, year.Status);

    public static GradeLevelResponse ToGrade(GradeLevel level) => new(
        level.Id, level.Code, level.Name, level.EducationLevel, level.SortOrder, level.IsActive);

    public static ClassroomResponse ToClassroom(Classroom room, int studentCount) => new(
        room.Id,
        room.SchoolId,
        room.SchoolYearId,
        room.SchoolYear.Name,
        room.GradeLevelId,
        room.GradeLevel.Name,
        room.ClassCode,
        room.Name,
        room.HomeroomTeacherUserId,
        room.HomeroomTeacher?.FullName,
        room.Capacity,
        studentCount,
        room.Status);

    public static StudentProfileResponse ToStudent(StudentProfile student)
    {
        var active = student.Enrollments
            .FirstOrDefault(item => item.Status == Domain.Common.EnrollmentStatus.Active && item.LeaveDate is null);
        return new StudentProfileResponse(
            student.Id,
            student.SchoolId,
            student.StudentCode,
            student.FullName,
            student.DateOfBirth,
            student.Gender,
            student.Email,
            student.Phone,
            student.Address,
            student.UserId,
            student.Status,
            active?.ClassroomId,
            active?.Classroom.ClassCode);
    }

    public static void ApplyStudent(StudentProfile student, SaveStudentProfileRequest request, DateTime now)
    {
        student.StudentCode = Code(request.StudentCode);
        student.FullName = request.FullName.Trim();
        student.DateOfBirth = request.DateOfBirth;
        student.Gender = request.Gender;
        student.Email = Empty(request.Email);
        student.Phone = Empty(request.Phone);
        student.Address = Empty(request.Address);
        student.UserId = request.UserId;
        student.Status = request.Status;
        student.UpdatedAtUtc = now;
    }

    public static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
