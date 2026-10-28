using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Exams;

// Tạo đề, chụp câu đã duyệt lúc phát hành, và quản lý thí sinh. Chưa chấm điểm.
public sealed class ExamService(
    IUnitOfWork unitOfWork,
    IClock clock,
    IValidator<SaveExamRequest> validator)
{
    public async Task<IReadOnlyList<ExamSummary>> ListAsync(CancellationToken ct) =>
        await unitOfWork.Exams.ListAsync(ct);

    public async Task<OperationResult<ExamResponse>> GetAsync(long id, CancellationToken ct)
    {
        var exam = await unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        return OperationResult<ExamResponse>.Ok(ExamMapper.ToResponse(exam));
    }

    public async Task<OperationResult<ExamResponse>> CreateAsync(SaveExamRequest request, Actor actor, CancellationToken ct)
    {
        Prepare(request);
        if (await validator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<ExamResponse>.Invalid(inputError);

        var subject = await unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null || !subject.IsActive)
            return OperationResult<ExamResponse>.Invalid(Messages.SubjectInactive);
        if (await ParticipantErrorAsync(request.ParticipantUserIds, ct) is { } participantError)
            return OperationResult<ExamResponse>.Invalid(participantError);

        var (questions, snapshotError) = await BuildSnapshotsAsync(request.Questions, subject.Id, ct);
        if (snapshotError is not null || questions is null)
            return OperationResult<ExamResponse>.Invalid(snapshotError ?? Messages.ExamQuestionNotApproved);

        var exam = new Exam
        {
            CreatedById = actor.UserId,
            CreatedAtUtc = clock.UtcNow,
            Status = ExamStatus.Draft,
            Subject = subject
        };
        ExamMapper.Apply(exam, request);
        foreach (var question in questions)
            exam.Questions.Add(question);
        foreach (var userId in request.ParticipantUserIds.Distinct())
            exam.Participants.Add(new ExamParticipant { UserId = userId });

        await unitOfWork.Exams.AddAsync(exam, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        var names = await ParticipantNamesAsync(exam, ct);
        return OperationResult<ExamResponse>.Ok(ExamMapper.ToResponse(exam, names));
    }

    // Chỉ đề nháp mới được đổi câu, điểm và danh sách thí sinh.
    public async Task<OperationResult<ExamResponse>> UpdateAsync(long id, SaveExamRequest request, Actor actor, CancellationToken ct)
    {
        Prepare(request);
        if (await validator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<ExamResponse>.Invalid(inputError);

        var exam = await unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        if (exam.Status != ExamStatus.Draft)
            return OperationResult<ExamResponse>.Invalid(Messages.ExamLocked);

        var subject = await unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null || !subject.IsActive)
            return OperationResult<ExamResponse>.Invalid(Messages.SubjectInactive);
        if (await ParticipantErrorAsync(request.ParticipantUserIds, ct) is { } participantError)
            return OperationResult<ExamResponse>.Invalid(participantError);

        var (questions, snapshotError) = await BuildSnapshotsAsync(request.Questions, subject.Id, ct);
        if (snapshotError is not null || questions is null)
            return OperationResult<ExamResponse>.Invalid(snapshotError ?? Messages.ExamQuestionNotApproved);

        ExamMapper.Apply(exam, request);
        exam.Subject = subject;
        unitOfWork.Exams.ReplaceQuestions(exam, questions);
        unitOfWork.Exams.ReplaceParticipants(exam, request.ParticipantUserIds.Distinct().ToArray());
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        var names = await ParticipantNamesAsync(exam, ct);
        return OperationResult<ExamResponse>.Ok(ExamMapper.ToResponse(exam, names));
    }

    // Phát hành thì chụp lại nội dung câu đang duyệt. Sửa ngân hàng sau đó không đổi đề này.
    public async Task<OperationResult<ExamResponse>> PublishAsync(long id, Actor actor, CancellationToken ct)
    {
        var exam = await unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        if (exam.Status != ExamStatus.Draft)
            return OperationResult<ExamResponse>.Invalid(Messages.ExamPublishNotDraft);
        if (exam.Questions.Count == 0)
            return OperationResult<ExamResponse>.Invalid(Messages.ExamPublishEmpty);

        var items = exam.Questions
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .Select(question => new ExamQuestionInput
            {
                QuestionId = question.SourceQuestionId ?? 0,
                Points = question.Points
            })
            .ToArray();
        var (questions, snapshotError) = await BuildSnapshotsAsync(items, exam.SubjectId, ct);
        if (snapshotError is not null || questions is null)
            return OperationResult<ExamResponse>.Invalid(snapshotError ?? Messages.ExamQuestionNotApproved);

        // Đề sinh theo profile phải còn đúng cơ cấu mức độ tại lúc phát hành.
        if (exam.DifficultyProfileId is long profileId)
        {
            var profile = await unitOfWork.ExamProfiles.FindAsync(profileId, ct);
            if (profile is null)
                return OperationResult<ExamResponse>.Invalid(Messages.ProfileNotFound);
            if (ExamMatrixCheck.Mismatch(profile, questions, exam.QuestionCountSnapshot) is { } mismatch)
                return OperationResult<ExamResponse>.Invalid(mismatch);
        }

        unitOfWork.Exams.ReplaceQuestions(exam, questions);
        exam.Status = ExamStatus.Published;
        exam.PublishedAtUtc = clock.UtcNow;
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.PublishExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        return OperationResult<ExamResponse>.Ok(ExamMapper.ToResponse(exam));
    }

    public async Task<OperationResult<ExamResponse>> CloseAsync(long id, Actor actor, CancellationToken ct)
    {
        var exam = await unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        if (exam.Status != ExamStatus.Published)
            return OperationResult<ExamResponse>.Invalid(Messages.ExamCloseNotPublished);

        exam.Status = ExamStatus.Closed;
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CloseExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        return OperationResult<ExamResponse>.Ok(ExamMapper.ToResponse(exam));
    }

    private async Task<(List<ExamQuestion>? Questions, string? Error)> BuildSnapshotsAsync(
        IReadOnlyList<ExamQuestionInput> items,
        long subjectId,
        CancellationToken ct)
    {
        var ids = items.Select(item => item.QuestionId).ToArray();
        if (ids.Distinct().Count() != ids.Length)
            return (null, Messages.ExamQuestionDuplicate);

        var sources = await unitOfWork.Questions.ListWithOptionsAsync(ids, ct);
        var byId = sources.ToDictionary(question => question.Id);
        var snapshots = new List<ExamQuestion>();
        var order = 1;
        foreach (var item in items)
        {
            // Câu chưa duyệt, khác môn, hoặc không đủ phương án thì không được vào đề.
            if (!byId.TryGetValue(item.QuestionId, out var source)
                || source.Status != QuestionStatus.Approved
                || source.Topic.SubjectId != subjectId
                || source.Options.Count < 2)
                return (null, Messages.ExamQuestionNotApproved);

            snapshots.Add(ExamMapper.Snapshot(source, item.Points, order++));
        }

        return (snapshots, null);
    }

    private async Task<string?> ParticipantErrorAsync(IReadOnlyList<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return null;
        if (ids.Distinct().Count() != ids.Count)
            return Messages.ExamParticipantDuplicate;

        var users = await unitOfWork.Users.ListByIdsAsync(ids, ct);
        var allowed = users.Where(IsActiveStudent).Select(user => user.Id).ToHashSet();
        return ids.All(allowed.Contains) ? null : Messages.ExamParticipantInvalid;
    }

    private async Task<IReadOnlyDictionary<long, User>> ParticipantNamesAsync(Exam exam, CancellationToken ct)
    {
        if (exam.Participants.Count == 0 || exam.Participants.All(participant => participant.User is not null))
            return exam.Participants.ToDictionary(participant => participant.UserId, participant => participant.User!);

        var users = await unitOfWork.Users.ListByIdsAsync(exam.Participants.Select(participant => participant.UserId).ToArray(), ct);
        return users.ToDictionary(user => user.Id);
    }

    private static bool IsActiveStudent(User user) =>
        user.Status == UserStatus.Active
        && user.UserRoles.Any(link => link.Role.Code == RoleCodes.Student);

    private static void Prepare(SaveExamRequest request)
    {
        request.Questions ??= [];
        request.ParticipantUserIds ??= [];
    }
}
