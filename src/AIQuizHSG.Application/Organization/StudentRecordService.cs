using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

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

// Hồ sơ học sinh không bắt buộc có tài khoản. Phân lớp đang hoạt động không được chồng nhau.
public sealed partial class StudentRecordService : TimedOrganizationService, IStudentRecordService
{
    private readonly IValidator<SaveStudentProfileRequest> validator;

    public StudentRecordService(
        IUnitOfWork unitOfWork,
        IClock clock,
        ISchoolAccess access,
        IValidator<SaveStudentProfileRequest> validator) : base(unitOfWork, clock, access)
    {
        this.validator = validator;
    }

    public async Task<OperationResult<IReadOnlyList<StudentProfileResponse>>> ListAsync(long schoolId, string? keyword, Actor actor, CancellationToken ct)
    {
        if (await unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<IReadOnlyList<StudentProfileResponse>>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "class.view", schoolId, ct) is { } denied)
            return OperationResult<IReadOnlyList<StudentProfileResponse>>.Forbidden(denied);
        if (!await access.IsWideAsync(actor.UserId, schoolId, ct))
            return OperationResult<IReadOnlyList<StudentProfileResponse>>.Forbidden(Messages.ClassScopeDenied);

        var students = await unitOfWork.StudentRecords.SearchAsync(schoolId, keyword, ct);
        return OperationResult<IReadOnlyList<StudentProfileResponse>>.Ok(students.Select(student =>
            new StudentProfileResponse(
                student.Id, student.SchoolId, student.StudentCode, student.FullName, student.DateOfBirth, student.Gender,
                student.Email, student.Phone, student.Address, student.UserId, student.Status, null, null)).ToArray());
    }

    public async Task<OperationResult<StudentProfileResponse>> CreateAsync(long schoolId, SaveStudentProfileRequest request, Actor actor, CancellationToken ct)
    {
        if (await unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<StudentProfileResponse>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "student.create", schoolId, ct) is { } denied)
            return OperationResult<StudentProfileResponse>.Forbidden(denied);
        if (await validator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<StudentProfileResponse>.Invalid(error);
        if (await CodeOrUserErrorAsync(schoolId, null, request, ct) is { } duplicate)
            return OperationResult<StudentProfileResponse>.Invalid(duplicate);

        var now = clock.UtcNow;
        var student = new StudentProfile { SchoolId = schoolId, CreatedAtUtc = now };
        OrganizationMapper.ApplyStudent(student, request, now);
        await unitOfWork.StudentRecords.AddAsync(student, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateStudentProfile, AuditCodes.StudentProfile, () => student.Id, student.StudentCode, actor.IpAddress, ct);
        return OperationResult<StudentProfileResponse>.Ok(OrganizationMapper.ToStudent(student));
    }

    public async Task<OperationResult<StudentProfileResponse>> UpdateAsync(long id, SaveStudentProfileRequest request, Actor actor, CancellationToken ct)
    {
        var student = await unitOfWork.StudentRecords.FindAsync(id, ct);
        if (student is null)
            return OperationResult<StudentProfileResponse>.NotFound(Messages.StudentProfileNotFound);
        if (await access.DenyAsync(actor.UserId, "student.create", student.SchoolId, ct) is { } denied)
            return OperationResult<StudentProfileResponse>.Forbidden(denied);
        if (await validator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<StudentProfileResponse>.Invalid(error);
        if (await CodeOrUserErrorAsync(student.SchoolId, student.Id, request, ct) is { } duplicate)
            return OperationResult<StudentProfileResponse>.Invalid(duplicate);

        OrganizationMapper.ApplyStudent(student, request, clock.UtcNow);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateStudentProfile, AuditCodes.StudentProfile, () => student.Id, student.StudentCode, actor.IpAddress, ct);
        return OperationResult<StudentProfileResponse>.Ok(OrganizationMapper.ToStudent(student));
    }

    public async Task<OperationResult<StudentProfileResponse>> EnrollAsync(long classroomId, EnrollStudentRequest request, Actor actor, CancellationToken ct)
    {
        var room = await unitOfWork.Classrooms.FindAsync(classroomId, ct);
        if (room is null)
            return OperationResult<StudentProfileResponse>.NotFound(Messages.ClassroomNotFound);
        if (await access.DenyAsync(actor.UserId, "student.create", room.SchoolId, ct) is { } denied)
            return OperationResult<StudentProfileResponse>.Forbidden(denied);

        var student = await unitOfWork.StudentRecords.FindAsync(request.StudentId, ct);
        if (student is null || student.SchoolId != room.SchoolId)
            return OperationResult<StudentProfileResponse>.Invalid(Messages.StudentProfileNotFound);
        if (student.Status != StudentProfileStatus.Active || room.Status != ClassroomStatus.Active)
            return OperationResult<StudentProfileResponse>.Invalid(Messages.ClassroomNotFound);
        if (await unitOfWork.StudentRecords.FindActiveEnrollmentAsync(student.Id, ct) is not null)
            return OperationResult<StudentProfileResponse>.Invalid(Messages.EnrollmentOverlap);
        if (await CapacityErrorAsync(room, ct) is { } full)
            return OperationResult<StudentProfileResponse>.Invalid(full);

        var enrollment = new StudentEnrollment
        {
            StudentId = student.Id,
            ClassroomId = room.Id,
            EnrollmentDate = request.EnrollmentDate ?? DateOnly.FromDateTime(clock.UtcNow),
            Status = EnrollmentStatus.Active,
            Note = OrganizationMapper.Empty(request.Note),
            CreatedAtUtc = clock.UtcNow,
            Classroom = room
        };
        student.Enrollments.Add(enrollment);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.EnrollStudent, AuditCodes.Enrollment, () => student.Id, room.ClassCode, actor.IpAddress, ct);
        return OperationResult<StudentProfileResponse>.Ok(OrganizationMapper.ToStudent(student));
    }

    // Đóng phân lớp cũ rồi tạo bản ghi mới để còn lịch sử.
    public async Task<OperationResult<StudentProfileResponse>> TransferAsync(long studentId, TransferStudentRequest request, Actor actor, CancellationToken ct)
    {
        var student = await unitOfWork.StudentRecords.FindAsync(studentId, ct);
        if (student is null)
            return OperationResult<StudentProfileResponse>.NotFound(Messages.StudentProfileNotFound);
        if (await access.DenyAsync(actor.UserId, "student.transfer", student.SchoolId, ct) is { } denied)
            return OperationResult<StudentProfileResponse>.Forbidden(denied);

        var current = await unitOfWork.StudentRecords.FindActiveEnrollmentAsync(student.Id, ct);
        if (current is null)
            return OperationResult<StudentProfileResponse>.Invalid(Messages.EnrollmentMissing);

        var room = await unitOfWork.Classrooms.FindAsync(request.ClassroomId, ct);
        if (room is null || room.SchoolId != student.SchoolId || room.Status != ClassroomStatus.Active)
            return OperationResult<StudentProfileResponse>.Invalid(Messages.ClassroomNotFound);
        if (room.Id == current.ClassroomId)
            return OperationResult<StudentProfileResponse>.Invalid(Messages.EnrollmentSameClass);
        if (await CapacityErrorAsync(room, ct) is { } full)
            return OperationResult<StudentProfileResponse>.Invalid(full);

        var day = request.TransferDate ?? DateOnly.FromDateTime(clock.UtcNow);
        if (day < current.EnrollmentDate)
            return OperationResult<StudentProfileResponse>.Invalid(Messages.SchoolYearDatesInvalid);

        current.LeaveDate = day;
        current.Status = EnrollmentStatus.Transferred;
        var next = new StudentEnrollment
        {
            StudentId = student.Id,
            ClassroomId = room.Id,
            EnrollmentDate = day,
            Status = EnrollmentStatus.Active,
            Note = OrganizationMapper.Empty(request.Note),
            CreatedAtUtc = clock.UtcNow,
            Classroom = room
        };
        student.Enrollments.Add(next);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.TransferStudent, AuditCodes.Enrollment, () => student.Id, room.ClassCode, actor.IpAddress, ct);
        return OperationResult<StudentProfileResponse>.Ok(OrganizationMapper.ToStudent(student));
    }

    public byte[] CreateImportTemplate() => StudentExcel.CreateTemplate();

    public async Task<OperationResult<ImportResult>> ImportAsync(long schoolId, long schoolYearId, Stream stream, Actor actor, CancellationToken ct)
    {
        if (await unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<ImportResult>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "student.import", schoolId, ct) is { } denied)
            return OperationResult<ImportResult>.Forbidden(denied);

        var year = await unitOfWork.Schools.FindYearAsync(schoolYearId, ct);
        if (year is null || year.SchoolId != schoolId)
            return OperationResult<ImportResult>.Invalid(Messages.SchoolYearWrongSchool);

        var read = StudentExcel.Read(stream);
        if (read.FatalMessage is not null)
            return OperationResult<ImportResult>.Invalid(read.FatalMessage);

        var errors = new List<ImportError>();
        var imported = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in read.Rows)
        {
            if (row.Error is not null)
            {
                errors.Add(new ImportError(row.RowNumber, row.Error));
                continue;
            }

            if (!seen.Add(row.StudentCode!))
            {
                errors.Add(new ImportError(row.RowNumber, Messages.StudentCodeExists));
                continue;
            }

            if (await unitOfWork.StudentRecords.FindByCodeAsync(schoolId, row.StudentCode!, ct) is not null)
            {
                errors.Add(new ImportError(row.RowNumber, Messages.StudentCodeExists));
                continue;
            }

            Classroom? room = null;
            if (row.ClassCode is not null)
            {
                room = await unitOfWork.Classrooms.FindByCodeAsync(schoolYearId, row.ClassCode, ct);
                if (room is null || room.SchoolId != schoolId)
                {
                    errors.Add(new ImportError(row.RowNumber, Messages.ClassroomNotFound));
                    continue;
                }
            }

            var now = clock.UtcNow;
            var student = new StudentProfile
            {
                SchoolId = schoolId,
                StudentCode = row.StudentCode!,
                FullName = row.FullName!,
                DateOfBirth = row.DateOfBirth,
                Gender = row.Gender,
                Email = row.Email,
                Phone = row.Phone,
                Address = row.Address,
                Status = StudentProfileStatus.Active,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            if (room is not null)
            {
                student.Enrollments.Add(new StudentEnrollment
                {
                    ClassroomId = room.Id,
                    EnrollmentDate = DateOnly.FromDateTime(now),
                    Status = EnrollmentStatus.Active,
                    CreatedAtUtc = now
                });
            }

            await unitOfWork.StudentRecords.AddAsync(student, ct);
            imported++;
        }

        if (imported > 0)
            await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.ImportStudents, AuditCodes.StudentProfile, () => schoolId, imported.ToString(), actor.IpAddress, ct);

        return OperationResult<ImportResult>.Ok(new ImportResult(imported, errors));
    }

    private async Task<string?> CodeOrUserErrorAsync(long schoolId, long? studentId, SaveStudentProfileRequest request, CancellationToken ct)
    {
        var code = OrganizationMapper.Code(request.StudentCode);
        var same = await unitOfWork.StudentRecords.FindByCodeAsync(schoolId, code, ct);
        if (same is not null && same.Id != studentId)
            return Messages.StudentCodeExists;
        if (request.UserId is null)
            return null;
        var user = await unitOfWork.Users.FindByIdAsync(request.UserId.Value, ct);
        if (user is null)
            return Messages.AccountInactive;
        var linked = await unitOfWork.StudentRecords.FindByUserAsync(request.UserId.Value, ct);
        if (linked is not null && linked.Id != studentId)
            return Messages.StudentUserTaken;
        return null;
    }

    private async Task<string?> CapacityErrorAsync(Classroom room, CancellationToken ct)
    {
        if (room.Capacity is null)
            return null;
        var count = await unitOfWork.Classrooms.CountActiveStudentsAsync(room.Id, ct);
        return count >= room.Capacity ? Messages.ClassroomFull : null;
    }
}
