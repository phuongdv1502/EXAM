using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Organization;

// Lớp thuộc đúng trường của năm học. Chủ nhiệm lưu trên lớp, bộ môn lưu ở phân công.
public sealed partial class ClassroomService : OrganizationService, IClassroomService
{
    private readonly IValidator<SaveClassroomRequest> validator;

    public ClassroomService(IUnitOfWork unitOfWork, ISchoolAccess access, IValidator<SaveClassroomRequest> validator)
        : base(unitOfWork, access)
    {
        this.validator = validator;
    }

    public async Task<OperationResult<IReadOnlyList<ClassroomResponse>>> ListAsync(long schoolId, long? schoolYearId, Actor actor, CancellationToken ct)
    {
        if (await _unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<IReadOnlyList<ClassroomResponse>>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "class.view", schoolId, ct) is { } denied)
            return OperationResult<IReadOnlyList<ClassroomResponse>>.Forbidden(denied);

        var rooms = await _unitOfWork.Classrooms.ListAsync(schoolId, schoolYearId, ct);
        if (!await access.IsWideAsync(actor.UserId, schoolId, ct))
        {
            var mine = new List<Classroom>();
            foreach (var room in rooms)
            {
                if (await _unitOfWork.Classrooms.TeachesAsync(room.Id, actor.UserId, ct))
                    mine.Add(room);
            }

            rooms = mine;
        }

        var result = new List<ClassroomResponse>();
        foreach (var room in rooms)
            result.Add(OrganizationMapper.ToClassroom(room, await _unitOfWork.Classrooms.CountActiveStudentsAsync(room.Id, ct)));
        return OperationResult<IReadOnlyList<ClassroomResponse>>.Ok(result);
    }

    public async Task<OperationResult<ClassroomResponse>> CreateAsync(long schoolId, SaveClassroomRequest request, Actor actor, CancellationToken ct)
    {
        if (await _unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<ClassroomResponse>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "school.manage", schoolId, ct) is { } denied)
            return OperationResult<ClassroomResponse>.Forbidden(denied);
        if (await validator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<ClassroomResponse>.Invalid(error);

        var year = await _unitOfWork.Schools.FindYearAsync(request.SchoolYearId, ct);
        if (year is null)
            return OperationResult<ClassroomResponse>.Invalid(Messages.SchoolYearNotFound);
        // Năm học của trường khác không được dùng để mở lớp.
        if (year.SchoolId != schoolId)
            return OperationResult<ClassroomResponse>.Invalid(Messages.SchoolYearWrongSchool);

        var grade = await _unitOfWork.GradeLevels.FindAsync(request.GradeLevelId, ct);
        if (grade is null || !grade.IsActive)
            return OperationResult<ClassroomResponse>.Invalid(Messages.GradeLevelNotFound);

        var code = OrganizationMapper.Code(request.ClassCode);
        if (await _unitOfWork.Classrooms.FindByCodeAsync(year.Id, code, ct) is not null)
            return OperationResult<ClassroomResponse>.Invalid(Messages.ClassroomCodeExists);
        if (await HomeroomErrorAsync(schoolId, request.HomeroomTeacherUserId, ct) is { } homeroomError)
            return OperationResult<ClassroomResponse>.Invalid(homeroomError);

        var room = new Classroom
        {
            SchoolId = schoolId,
            SchoolYearId = year.Id,
            GradeLevelId = grade.Id,
            ClassCode = code,
            Name = request.Name.Trim(),
            HomeroomTeacherUserId = request.HomeroomTeacherUserId,
            Capacity = request.Capacity,
            Status = request.Status,
            SchoolYear = year,
            GradeLevel = grade
        };
        await _unitOfWork.Classrooms.AddAsync(room, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateClassroom, AuditCodes.Classroom, () => room.Id, room.ClassCode, actor.IpAddress, ct);
        return OperationResult<ClassroomResponse>.Ok(OrganizationMapper.ToClassroom(room, 0));
    }

    public async Task<OperationResult<ClassroomResponse>> UpdateAsync(long id, SaveClassroomRequest request, Actor actor, CancellationToken ct)
    {
        var room = await _unitOfWork.Classrooms.FindAsync(id, ct);
        if (room is null)
            return OperationResult<ClassroomResponse>.NotFound(Messages.ClassroomNotFound);
        if (await access.DenyAsync(actor.UserId, "school.manage", room.SchoolId, ct) is { } denied)
            return OperationResult<ClassroomResponse>.Forbidden(denied);
        if (await validator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<ClassroomResponse>.Invalid(error);

        var year = await _unitOfWork.Schools.FindYearAsync(request.SchoolYearId, ct);
        if (year is null || year.SchoolId != room.SchoolId)
            return OperationResult<ClassroomResponse>.Invalid(Messages.SchoolYearWrongSchool);
        var grade = await _unitOfWork.GradeLevels.FindAsync(request.GradeLevelId, ct);
        if (grade is null || !grade.IsActive)
            return OperationResult<ClassroomResponse>.Invalid(Messages.GradeLevelNotFound);

        var code = OrganizationMapper.Code(request.ClassCode);
        var same = await _unitOfWork.Classrooms.FindByCodeAsync(year.Id, code, ct);
        if (same is not null && same.Id != id)
            return OperationResult<ClassroomResponse>.Invalid(Messages.ClassroomCodeExists);
        if (await HomeroomErrorAsync(room.SchoolId, request.HomeroomTeacherUserId, ct) is { } homeroomError)
            return OperationResult<ClassroomResponse>.Invalid(homeroomError);

        room.SchoolYearId = year.Id;
        room.GradeLevelId = grade.Id;
        room.ClassCode = code;
        room.Name = request.Name.Trim();
        room.HomeroomTeacherUserId = request.HomeroomTeacherUserId;
        room.Capacity = request.Capacity;
        room.Status = request.Status;
        room.SchoolYear = year;
        room.GradeLevel = grade;
        var count = await _unitOfWork.Classrooms.CountActiveStudentsAsync(room.Id, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateClassroom, AuditCodes.Classroom, () => room.Id, room.ClassCode, actor.IpAddress, ct);
        return OperationResult<ClassroomResponse>.Ok(OrganizationMapper.ToClassroom(room, count));
    }

    public async Task<OperationResult<ClassTeacherResponse>> AssignTeacherAsync(long classroomId, AssignTeacherRequest request, Actor actor, CancellationToken ct)
    {
        var room = await _unitOfWork.Classrooms.FindAsync(classroomId, ct);
        if (room is null)
            return OperationResult<ClassTeacherResponse>.NotFound(Messages.ClassroomNotFound);
        if (await access.DenyAsync(actor.UserId, "school.manage", room.SchoolId, ct) is { } denied)
            return OperationResult<ClassTeacherResponse>.Forbidden(denied);
        if (request.SubjectId <= 0)
            return OperationResult<ClassTeacherResponse>.Invalid(Messages.TeacherSubjectRequired);

        var subject = await _unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null || !subject.IsActive)
            return OperationResult<ClassTeacherResponse>.Invalid(Messages.SubjectInactive);
        if (await HomeroomErrorAsync(room.SchoolId, request.UserId, ct) is { } memberError)
            return OperationResult<ClassTeacherResponse>.Invalid(memberError);
        if (await _unitOfWork.Classrooms.FindSubjectTeacherAsync(classroomId, request.UserId, request.SubjectId, ct) is not null)
            return OperationResult<ClassTeacherResponse>.Invalid(Messages.TeacherAssignmentExists);

        var link = new ClassMembership
        {
            ClassroomId = classroomId,
            UserId = request.UserId,
            ClassRole = ClassRole.SubjectTeacher,
            SubjectId = request.SubjectId
        };
        await _unitOfWork.Classrooms.AddTeacherAsync(link, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.AssignTeacher, AuditCodes.Classroom, () => classroomId, subject.Code, actor.IpAddress, ct);
        var user = await _unitOfWork.Users.FindByIdAsync(request.UserId, ct);
        return OperationResult<ClassTeacherResponse>.Ok(new ClassTeacherResponse(request.UserId, user?.FullName ?? string.Empty, subject.Id, subject.Name));
    }

    public async Task<OperationResult<IReadOnlyList<StudentProfileResponse>>> ListStudentsAsync(long classroomId, Actor actor, CancellationToken ct)
    {
        var room = await _unitOfWork.Classrooms.FindAsync(classroomId, ct);
        if (room is null)
            return OperationResult<IReadOnlyList<StudentProfileResponse>>.NotFound(Messages.ClassroomNotFound);
        if (await access.DenyClassroomAsync(actor.UserId, room, "class.view", ct) is { } denied)
            return OperationResult<IReadOnlyList<StudentProfileResponse>>.Forbidden(denied);

        var students = await _unitOfWork.StudentRecords.ListActiveInClassroomAsync(classroomId, ct);
        return OperationResult<IReadOnlyList<StudentProfileResponse>>.Ok(students.Select(student =>
            new StudentProfileResponse(
                student.Id, student.SchoolId, student.StudentCode, student.FullName, student.DateOfBirth, student.Gender,
                student.Email, student.Phone, student.Address, student.UserId, student.Status, classroomId, room.ClassCode)).ToArray());
    }

    // Chủ nhiệm và giáo viên bộ môn đều phải là thành viên đang hoạt động của trường.
    private async Task<string?> HomeroomErrorAsync(long schoolId, long? userId, CancellationToken ct)
    {
        if (userId is null)
            return null;
        var user = await _unitOfWork.Users.FindByIdAsync(userId.Value, ct);
        if (user is null || user.Status != UserStatus.Active)
            return Messages.AccountInactive;
        if (!await _unitOfWork.Schools.HasActiveMembershipAsync(userId.Value, schoolId, ct))
            return Messages.HomeroomNotInSchool;
        return null;
    }
}
