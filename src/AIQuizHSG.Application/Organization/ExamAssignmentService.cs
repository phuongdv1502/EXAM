using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Organization;

public interface IExamAssignmentService
{
    Task<OperationResult<ExamAssignmentResponse>> AssignAsync(long examId, AssignExamRequest request, Actor actor, CancellationToken ct);
}

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

    public async Task<OperationResult<ExamAssignmentResponse>> AssignAsync(long examId, AssignExamRequest request, Actor actor, CancellationToken ct)
    {
        if (await validator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<ExamAssignmentResponse>.Invalid(error);

        var exam = await unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<ExamAssignmentResponse>.NotFound(Messages.ExamNotFound);

        long schoolId;
        if (request.ClassroomId is { } classroomId)
        {
            var room = await unitOfWork.Classrooms.FindAsync(classroomId, ct);
            if (room is null)
                return OperationResult<ExamAssignmentResponse>.Invalid(Messages.ClassroomNotFound);
            if (await access.DenyClassroomAsync(actor.UserId, room, "exam.assign", ct) is { } denied)
                return OperationResult<ExamAssignmentResponse>.Forbidden(denied);
            schoolId = room.SchoolId;
        }
        else
        {
            var student = await unitOfWork.StudentRecords.FindAsync(request.StudentId!.Value, ct);
            if (student is null)
                return OperationResult<ExamAssignmentResponse>.Invalid(Messages.StudentProfileNotFound);
            if (await access.DenyAsync(actor.UserId, "exam.assign", student.SchoolId, ct) is { } denied)
                return OperationResult<ExamAssignmentResponse>.Forbidden(denied);
            var wide = await access.IsWideAsync(actor.UserId, student.SchoolId, ct);
            var active = student.Enrollments.FirstOrDefault(item => item.Status == EnrollmentStatus.Active && item.LeaveDate is null);
            if (!wide && (active is null || !await unitOfWork.Classrooms.TeachesAsync(active.ClassroomId, actor.UserId, ct)))
                return OperationResult<ExamAssignmentResponse>.Forbidden(Messages.ClassScopeDenied);
            schoolId = student.SchoolId;
        }

        if (await unitOfWork.ExamAssignments.FindActiveAsync(examId, request.ClassroomId, request.StudentId, ct) is not null)
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
        await unitOfWork.ExamAssignments.AddAsync(assignment, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.AssignExam, AuditCodes.ExamAssignment, () => assignment.Id, schoolId.ToString(), actor.IpAddress, ct);
        return OperationResult<ExamAssignmentResponse>.Ok(new ExamAssignmentResponse(
            assignment.Id, examId, assignment.ClassroomId, assignment.StudentId, assignment.AssignedAtUtc, assignment.DueAtUtc));
    }
}
