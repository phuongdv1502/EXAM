using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Organization;

// Đề dùng chung nhiều trường. Phạm vi làm bài nằm ở bản giao, không gắn cứng vào đề.
public sealed partial class ExamAssignmentService : TimedOrganizationService, IExamAssignmentService
{
    private readonly IValidator<AssignExamRequest> validator;

    public ExamAssignmentService(
        IUnitOfWork unitOfWork,
        IClock clock,
        ISchoolAccess access,
        IValidator<AssignExamRequest> validator) : base(unitOfWork, clock, access)
    {
        this.validator = validator;
    }

    // Liệt kê lớp và học sinh đã được giao đề này.
    public async Task<OperationResult<IReadOnlyList<ExamAssignmentResponse>>> ListAsync(long examId, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "exam.assign", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<ExamAssignmentResponse>>.Forbidden(denied);
        if (await _unitOfWork.Exams.FindAsync(examId, ct) is null)
            return OperationResult<IReadOnlyList<ExamAssignmentResponse>>.NotFound(Messages.ExamNotFound);

        var rows = await _unitOfWork.ExamAssignments.ListByExamAsync(examId, ct);
        return OperationResult<IReadOnlyList<ExamAssignmentResponse>>.Ok(rows.Select(ToResponse).ToList());
    }

    public async Task<OperationResult<ExamAssignmentResponse>> AssignAsync(long examId, AssignExamRequest request, Actor actor, CancellationToken ct)
    {
        if (await validator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<ExamAssignmentResponse>.Invalid(error);

        var exam = await _unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<ExamAssignmentResponse>.NotFound(Messages.ExamNotFound);

        long schoolId;
        string? classroomName = null;
        string? studentName = null;
        if (request.ClassroomId is { } classroomId)
        {
            var room = await _unitOfWork.Classrooms.FindAsync(classroomId, ct);
            if (room is null)
                return OperationResult<ExamAssignmentResponse>.Invalid(Messages.ClassroomNotFound);
            if (await access.DenyClassroomAsync(actor.UserId, room, "exam.assign", ct) is { } denied)
                return OperationResult<ExamAssignmentResponse>.Forbidden(denied);
            schoolId = room.SchoolId;
            classroomName = room.Name;
        }
        else
        {
            var student = await _unitOfWork.StudentRecords.FindAsync(request.StudentId!.Value, ct);
            if (student is null)
                return OperationResult<ExamAssignmentResponse>.Invalid(Messages.StudentProfileNotFound);
            if (await access.DenyAsync(actor.UserId, "exam.assign", student.SchoolId, ct) is { } denied)
                return OperationResult<ExamAssignmentResponse>.Forbidden(denied);
            var wide = await access.IsWideAsync(actor.UserId, student.SchoolId, ct);
            var active = student.Enrollments.FirstOrDefault(item => item.Status == EnrollmentStatus.Active && item.LeaveDate is null);
            if (!wide && (active is null || !await _unitOfWork.Classrooms.TeachesAsync(active.ClassroomId, actor.UserId, ct)))
                return OperationResult<ExamAssignmentResponse>.Forbidden(Messages.ClassScopeDenied);
            schoolId = student.SchoolId;
            studentName = student.FullName;
        }

        if (await _unitOfWork.ExamAssignments.FindActiveAsync(examId, request.ClassroomId, request.StudentId, ct) is not null)
            return OperationResult<ExamAssignmentResponse>.Invalid(Messages.AssignmentExists);

        var assignment = new ExamAssignment
        {
            ExamId = examId,
            ClassroomId = request.ClassroomId,
            StudentId = request.StudentId,
            AssignedByUserId = actor.UserId,
            AssignedAtUtc = clock.UtcNow,
            DueAtUtc = request.DueAtUtc,
            Status = AssignmentStatus.Active
        };
        await _unitOfWork.ExamAssignments.AddAsync(assignment, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.AssignExam, AuditCodes.ExamAssignment, () => assignment.Id, schoolId.ToString(), actor.IpAddress, ct);
        return OperationResult<ExamAssignmentResponse>.Ok(new ExamAssignmentResponse(
            assignment.Id, examId, assignment.ClassroomId, assignment.StudentId, assignment.AssignedAtUtc, assignment.DueAtUtc, classroomName, studentName));
    }

    private static ExamAssignmentResponse ToResponse(ExamAssignment assignment) => new(
        assignment.Id,
        assignment.ExamId,
        assignment.ClassroomId,
        assignment.StudentId,
        assignment.AssignedAtUtc,
        assignment.DueAtUtc,
        assignment.Classroom?.Name,
        assignment.Student?.FullName);
}
