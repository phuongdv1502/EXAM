using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Exams;

// Học sinh vào đề, lưu đáp án và nộp bài. Hết giờ thì máy chủ tự nộp. Chưa tính điểm.
public sealed partial class AttemptService : TimedService, IAttemptService
{
    private readonly IValidator<SaveAnswerRequest> validator;

    public AttemptService(IUnitOfWork unitOfWork, IClock clock, IValidator<SaveAnswerRequest> validator) : base(unitOfWork, clock)
    {
        this.validator = validator;
    }

    public async Task<IReadOnlyList<ExamSummary>> ListOpenAsync(long userId, CancellationToken ct) =>
        await _unitOfWork.Exams.ListOpenForStudentAsync(userId, ct);

    public async Task<IReadOnlyList<AttemptSummary>> ListMineAsync(long userId, CancellationToken ct)
    {
        var attempts = await _unitOfWork.Attempts.ListByUserAsync(userId, ct);
        return attempts.Select(ExamMapper.ToSummary).ToArray();
    }

    public async Task<OperationResult<AttemptPaper>> StartAsync(long examId, Actor actor, CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<AttemptPaper>.NotFound(Messages.ExamNotFound);
        if (!IsOpen(exam, clock.UtcNow))
            return OperationResult<AttemptPaper>.Invalid(Messages.ExamNotOpen);

        // Không khai báo thí sinh thì mọi học sinh được vào. Có danh sách thì phải nằm trong đó.
        if (exam.Participants.Count > 0 && exam.Participants.All(participant => participant.UserId != actor.UserId))
            return OperationResult<AttemptPaper>.Invalid(Messages.ExamNotParticipant);
        // Có giao đề cho lớp hoặc học sinh thì chỉ hồ sơ nằm trong phạm vi đó mới được vào.
        if (!await _unitOfWork.ExamAssignments.AllowsAsync(exam.Id, actor.UserId, ct))
            return OperationResult<AttemptPaper>.Invalid(Messages.ExamNotParticipant);

        var existing = await _unitOfWork.Attempts.FindInProgressAsync(exam.Id, actor.UserId, ct);
        if (existing is not null)
            return OperationResult<AttemptPaper>.Ok(ExamMapper.ToPaper(existing, clock.UtcNow));

        var count = await _unitOfWork.Attempts.CountAsync(exam.Id, actor.UserId, ct);
        if (count >= exam.MaxAttempts)
            return OperationResult<AttemptPaper>.Invalid(Messages.ExamAttemptLimit);

        var now = clock.UtcNow;
        var due = now.AddMinutes(exam.DurationMinutes);
        if (exam.ClosesAtUtc is { } closes && closes < due)
            due = closes;

        var attempt = new ExamAttempt
        {
            Exam = exam,
            ExamId = exam.Id,
            UserId = actor.UserId,
            AttemptNumber = count + 1,
            StartedAtUtc = now,
            DueAtUtc = due,
            Status = AttemptStatus.InProgress
        };
        await _unitOfWork.Attempts.AddAsync(attempt, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.StartAttempt, AuditCodes.Attempt, () => attempt.Id, exam.Id.ToString(), actor.IpAddress, ct);
        return OperationResult<AttemptPaper>.Ok(ExamMapper.ToPaper(attempt, now));
    }

    public async Task<OperationResult<AttemptPaper>> GetAsync(long id, Actor actor, CancellationToken ct)
    {
        var attempt = await OwnedAsync(id, actor.UserId, ct);
        if (attempt is null)
            return OperationResult<AttemptPaper>.NotFound(Messages.AttemptNotFound);

        await SubmitIfDueAsync(attempt, actor, ct);
        return OperationResult<AttemptPaper>.Ok(ExamMapper.ToPaper(attempt, clock.UtcNow));
    }

    public async Task<OperationResult<AttemptPaper>> SaveAnswerAsync(
        long id,
        long examQuestionId,
        SaveAnswerRequest request,
        Actor actor,
        CancellationToken ct)
    {
        request.OptionIds ??= [];
        if (await validator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<AttemptPaper>.Invalid(inputError);

        var attempt = await OwnedAsync(id, actor.UserId, ct);
        if (attempt is null)
            return OperationResult<AttemptPaper>.NotFound(Messages.AttemptNotFound);
        if (attempt.Status != AttemptStatus.InProgress)
            return OperationResult<AttemptPaper>.Invalid(Messages.AttemptFinished);
        // Quá hạn thì nộp phần đã lưu, không nhận lựa chọn mới.
        if (await SubmitIfDueAsync(attempt, actor, ct))
            return OperationResult<AttemptPaper>.Invalid(Messages.AttemptFinished);

        var question = attempt.Exam.Questions.FirstOrDefault(item => item.Id == examQuestionId);
        if (question is null)
            return OperationResult<AttemptPaper>.Invalid(Messages.AttemptQuestionInvalid);
        if (request.OptionIds.Distinct().Count() != request.OptionIds.Count)
            return OperationResult<AttemptPaper>.Invalid(Messages.AttemptOptionInvalid);
        if (question.Type == QuestionType.SingleChoice && request.OptionIds.Count > 1)
            return OperationResult<AttemptPaper>.Invalid(Messages.AttemptSingleChoice);

        var allowed = question.Options.Select(option => option.Id).ToHashSet();
        if (request.OptionIds.Any(optionId => !allowed.Contains(optionId)))
            return OperationResult<AttemptPaper>.Invalid(Messages.AttemptOptionInvalid);

        var answer = attempt.Answers.FirstOrDefault(item => item.ExamQuestionId == examQuestionId);
        if (answer is null)
        {
            answer = new AttemptAnswer { ExamQuestionId = examQuestionId, UpdatedAtUtc = clock.UtcNow };
            attempt.Answers.Add(answer);
        }
        else
        {
            answer.UpdatedAtUtc = clock.UtcNow;
        }

        _unitOfWork.Attempts.ReplaceSelections(answer, request.OptionIds);
        await _unitOfWork.SaveChangesAsync(ct);
        return OperationResult<AttemptPaper>.Ok(ExamMapper.ToPaper(attempt, clock.UtcNow));
    }

    // Nộp lần hai trả về cùng một lượt, không tạo kết quả mới. Điểm vẫn để trống.
    public async Task<OperationResult<AttemptSummary>> SubmitAsync(long id, Actor actor, CancellationToken ct)
    {
        var attempt = await OwnedAsync(id, actor.UserId, ct);
        if (attempt is null)
            return OperationResult<AttemptSummary>.NotFound(Messages.AttemptNotFound);
        if (attempt.Status == AttemptStatus.Submitted)
            return OperationResult<AttemptSummary>.Ok(ExamMapper.ToSummary(attempt));
        if (attempt.Status != AttemptStatus.InProgress)
            return OperationResult<AttemptSummary>.Invalid(Messages.AttemptFinished);

        var now = clock.UtcNow;
        MarkSubmitted(attempt, now >= attempt.DueAtUtc ? attempt.DueAtUtc : now);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.SubmitAttempt, AuditCodes.Attempt, () => attempt.Id, null, actor.IpAddress, ct);
        return OperationResult<AttemptSummary>.Ok(ExamMapper.ToSummary(attempt));
    }

    private async Task<bool> SubmitIfDueAsync(ExamAttempt attempt, Actor actor, CancellationToken ct)
    {
        if (attempt.Status != AttemptStatus.InProgress || clock.UtcNow < attempt.DueAtUtc)
            return false;

        MarkSubmitted(attempt, attempt.DueAtUtc);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.SubmitAttempt, AuditCodes.Attempt, () => attempt.Id, null, actor.IpAddress, ct);
        return true;
    }

    // Chỉ ghi đã nộp. Quy tắc chấm điểm sẽ tính sau, không nhận điểm từ client.
    private static void MarkSubmitted(ExamAttempt attempt, DateTime submittedAt)
    {
        attempt.Status = AttemptStatus.Submitted;
        attempt.SubmittedAtUtc = submittedAt;
    }

    private async Task<ExamAttempt?> OwnedAsync(long id, long userId, CancellationToken ct)
    {
        var attempt = await _unitOfWork.Attempts.FindAsync(id, ct);
        return attempt is null || attempt.UserId != userId ? null : attempt;
    }

    private static bool IsOpen(Exam exam, DateTime now)
    {
        if (exam.Status != ExamStatus.Published || exam.Questions.Count == 0)
            return false;
        if (exam.OpensAtUtc is { } opens && now < opens)
            return false;
        if (exam.ClosesAtUtc is { } closes && now >= closes)
            return false;
        return true;
    }
}
