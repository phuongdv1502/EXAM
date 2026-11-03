using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Omr;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Exams;
using FluentValidation;

namespace AIQuizHSG.Application.Exams;

// Tạo đề, chụp câu đã duyệt lúc phát hành, và quản lý thí sinh. Chưa chấm điểm.
public sealed partial class ExamService : TimedService, IExamService
{
    private readonly IValidator<SaveExamRequest> validator;
    private readonly IExamPaperRenderer papers;

    public ExamService(
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<SaveExamRequest> validator,
        IExamPaperRenderer papers) : base(unitOfWork, clock)
    {
        this.validator = validator;
        this.papers = papers;
    }

    public async Task<IReadOnlyList<ExamSummary>> ListAsync(CancellationToken ct) =>
        await _unitOfWork.Exams.ListAsync(ct);

    public async Task<OperationResult<ExamResponse>> GetAsync(long id, CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        return OperationResult<ExamResponse>.Ok(await ToExamResponseAsync(exam, null, ct));
    }

    public async Task<OperationResult<ExamResponse>> CreateAsync(SaveExamRequest request, Actor actor, CancellationToken ct)
    {
        Prepare(request);
        if (await validator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<ExamResponse>.Invalid(inputError);

        var subject = await _unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null || !subject.IsActive)
            return OperationResult<ExamResponse>.Invalid(Messages.SubjectInactive);
        if (await TeachingGuard.DenyAsync(_unitOfWork, actor.UserId, subject.Id, ct) is { } denied)
            return OperationResult<ExamResponse>.Invalid(denied);
        if (await ParticipantErrorAsync(request.ParticipantUserIds, ct) is { } participantError)
            return OperationResult<ExamResponse>.Invalid(participantError);

        var (questions, snapshotError) = await BuildSnapshotsAsync(request.Questions, subject.Id, ct);
        if (snapshotError is not null || questions is null)
            return OperationResult<ExamResponse>.Invalid(snapshotError ?? Messages.ExamQuestionNotApproved);
        if (await SheetErrorAsync(request.OmrTemplateId, questions, exact: false, ct) is { } sheetError)
            return OperationResult<ExamResponse>.Invalid(sheetError);

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

        await _unitOfWork.Exams.AddAsync(exam, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        var names = await ParticipantNamesAsync(exam, ct);
        return OperationResult<ExamResponse>.Ok(await ToExamResponseAsync(exam, names, ct));
    }

    // Nháp luôn sửa được. Đề đã phát hành/đóng vẫn sửa được nếu chưa có phiếu chấm.
    public async Task<OperationResult<ExamResponse>> UpdateAsync(long id, SaveExamRequest request, Actor actor, CancellationToken ct)
    {
        Prepare(request);
        if (await validator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<ExamResponse>.Invalid(inputError);

        var exam = await _unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        if (!await CanEditContentAsync(exam, ct))
            return OperationResult<ExamResponse>.Invalid(
                exam.Status == ExamStatus.Draft ? Messages.ExamLocked : Messages.ExamEditBlockedByOmr);

        var subject = await _unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null || !subject.IsActive)
            return OperationResult<ExamResponse>.Invalid(Messages.SubjectInactive);
        if (await TeachingGuard.DenyAsync(_unitOfWork, actor.UserId, subject.Id, ct) is { } denied)
            return OperationResult<ExamResponse>.Invalid(denied);
        if (await ParticipantErrorAsync(request.ParticipantUserIds, ct) is { } participantError)
            return OperationResult<ExamResponse>.Invalid(participantError);

        var (questions, snapshotError) = await BuildSnapshotsAsync(request.Questions, subject.Id, ct);
        if (snapshotError is not null || questions is null)
            return OperationResult<ExamResponse>.Invalid(snapshotError ?? Messages.ExamQuestionNotApproved);
        // Đề đã phát hành thì mẫu phải khớp đủ câu từng phần.
        var exactSheet = exam.Status != ExamStatus.Draft;
        if (await SheetErrorAsync(request.OmrTemplateId, questions, exact: exactSheet, ct) is { } sheetError)
            return OperationResult<ExamResponse>.Invalid(sheetError);

        ExamMapper.Apply(exam, request);
        exam.Subject = subject;
        _unitOfWork.Exams.ReplaceQuestions(exam, questions);
        _unitOfWork.Exams.ReplaceParticipants(exam, request.ParticipantUserIds.Distinct().ToArray());
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        var names = await ParticipantNamesAsync(exam, ct);
        return OperationResult<ExamResponse>.Ok(await ToExamResponseAsync(exam, names, ct));
    }

    // Phát hành thì chụp lại nội dung câu đang duyệt. Sửa ngân hàng sau đó không đổi đề này.
    public async Task<OperationResult<ExamResponse>> PublishAsync(long id, Actor actor, CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(id, ct);
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
        // Phát hành thì đề phải đủ câu đúng từng phần của mẫu đã chọn.
        if (await SheetErrorAsync(exam.OmrTemplateId, questions, exact: true, ct) is { } sheetError)
            return OperationResult<ExamResponse>.Invalid(sheetError);

        // Đề sinh theo profile phải còn đúng cơ cấu mức độ tại lúc phát hành.
        if (exam.DifficultyProfileId is long profileId)
        {
            var profile = await _unitOfWork.ExamProfiles.FindAsync(profileId, ct);
            if (profile is null)
                return OperationResult<ExamResponse>.Invalid(Messages.ProfileNotFound);
            if (ExamMatrixCheck.Mismatch(profile, questions, exam.QuestionCountSnapshot) is { } mismatch)
                return OperationResult<ExamResponse>.Invalid(mismatch);
        }

        _unitOfWork.Exams.ReplaceQuestions(exam, questions);
        exam.Status = ExamStatus.Published;
        exam.PublishedAtUtc = clock.UtcNow;
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.PublishExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        return OperationResult<ExamResponse>.Ok(await ToExamResponseAsync(exam, null, ct));
    }

    public async Task<OperationResult<ExamResponse>> CloseAsync(long id, Actor actor, CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        if (exam.Status != ExamStatus.Published)
            return OperationResult<ExamResponse>.Invalid(Messages.ExamCloseNotPublished);

        exam.Status = ExamStatus.Closed;
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CloseExam, AuditCodes.Exam, () => exam.Id, null, actor.IpAddress, ct);
        return OperationResult<ExamResponse>.Ok(await ToExamResponseAsync(exam, null, ct));
    }

    // Đề đã phát hành mà quên gắn mẫu vẫn gắn được, miễn chưa có phiếu chấm.
    public async Task<OperationResult<ExamResponse>> AttachSheetAsync(
        long id,
        AttachExamSheetRequest request,
        Actor actor,
        CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(id, ct);
        if (exam is null)
            return OperationResult<ExamResponse>.NotFound(Messages.ExamNotFound);
        if (await TeachingGuard.DenyAsync(_unitOfWork, actor.UserId, exam.SubjectId, ct) is { } denied)
            return OperationResult<ExamResponse>.Forbidden(denied);
        if (exam.Status is not (ExamStatus.Published or ExamStatus.Closed or ExamStatus.Draft))
            return OperationResult<ExamResponse>.Invalid(Messages.ExamLocked);
        if (request.OmrTemplateId < 1)
            return OperationResult<ExamResponse>.Invalid(Messages.ExamSheetPickRequired);

        // Đã có phiếu trong đợt thì không đổi mẫu để tránh lệch tọa độ đã quét.
        if (exam.OmrTemplateId is long current && current != request.OmrTemplateId)
        {
            var busy = (await _unitOfWork.Omr.ListBatchesAsync(ct))
                .Any(item => item.ExamId == exam.Id && (item.TotalSheets > 0 || item.ProcessedSheets > 0 || item.Sheets.Count > 0));
            if (busy)
                return OperationResult<ExamResponse>.Invalid(Messages.ExamSheetChangeBlocked);
        }

        var questions = exam.Questions.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).ToList();
        if (await SheetErrorAsync(request.OmrTemplateId, questions, exact: true, ct) is { } sheetError)
            return OperationResult<ExamResponse>.Invalid(sheetError);

        exam.OmrTemplateId = request.OmrTemplateId;
        exam.ShuffleQuestions = false;
        exam.ShuffleOptions = false;
        await _unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.AttachExamSheet,
            AuditCodes.Exam,
            () => exam.Id,
            request.OmrTemplateId.ToString(),
            actor.IpAddress,
            ct);
        return OperationResult<ExamResponse>.Ok(await ToExamResponseAsync(exam, null, ct));
    }

    // Nháp luôn sửa. Đã phát hành thì chỉ sửa khi chưa có phiếu trong đợt chấm.
    private async Task<bool> CanEditContentAsync(Exam exam, CancellationToken ct)
    {
        if (exam.Status == ExamStatus.Draft)
            return true;
        if (exam.Status is not (ExamStatus.Published or ExamStatus.Closed))
            return false;
        return !await HasOmrSheetActivityAsync(exam.Id, ct);
    }

    private async Task<bool> HasOmrSheetActivityAsync(long examId, CancellationToken ct)
    {
        var batches = await _unitOfWork.Omr.ListBatchesAsync(ct);
        return batches.Any(item =>
            item.ExamId == examId
            && (item.TotalSheets > 0 || item.ProcessedSheets > 0 || item.Sheets.Count > 0));
    }

    private async Task<ExamResponse> ToExamResponseAsync(
        Exam exam,
        IReadOnlyDictionary<long, User>? users,
        CancellationToken ct)
    {
        var canEdit = await CanEditContentAsync(exam, ct);
        return ExamMapper.ToResponse(exam, users, canEdit);
    }

    private async Task<(List<ExamQuestion>? Questions, string? Error)> BuildSnapshotsAsync(
        IReadOnlyList<ExamQuestionInput> items,
        long subjectId,
        CancellationToken ct)
    {
        var ids = items.Select(item => item.QuestionId).ToArray();
        if (ids.Distinct().Count() != ids.Length)
            return (null, Messages.ExamQuestionDuplicate);

        var sources = await _unitOfWork.Questions.ListWithOptionsAsync(ids, ct);
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

        var users = await _unitOfWork.Users.ListByIdsAsync(ids, ct);
        var allowed = users.Where(IsActiveStudent).Select(user => user.Id).ToHashSet();
        return ids.All(allowed.Contains) ? null : Messages.ExamParticipantInvalid;
    }

    private async Task<IReadOnlyDictionary<long, User>> ParticipantNamesAsync(Exam exam, CancellationToken ct)
    {
        if (exam.Participants.Count == 0 || exam.Participants.All(participant => participant.User is not null))
            return exam.Participants.ToDictionary(participant => participant.UserId, participant => participant.User!);

        var users = await _unitOfWork.Users.ListByIdsAsync(exam.Participants.Select(participant => participant.UserId).ToArray(), ct);
        return users.ToDictionary(user => user.Id);
    }

    private static bool IsActiveStudent(User user) =>
        user.Status == UserStatus.Active
        && user.UserRoles.Any(link => link.Role.Code == RoleCodes.Student);

    private async Task<string?> SheetErrorAsync(long? templateId, IReadOnlyList<ExamQuestion> questions, bool exact, CancellationToken ct)
    {
        if (templateId is not long id)
            return null;

        var template = await _unitOfWork.Omr.FindTemplateAsync(id, ct);
        if (template is null || !template.IsActive)
            return Messages.ExamSheetMissing;
        if (!OmrTemplateConfig.TryParse(template.ConfigJson, out var config))
            return Messages.OmrTemplateConfigInvalid;

        // Xếp theo phần mẫu trước khi đối chiếu, tránh báo lệch chỉ vì thứ tự kéo thả.
        var ordered = OrderQuestionsForSheet(questions, config);
        var counts = ordered.Select(question => question.Options.Count).ToList();
        if (!config.Accepts(counts, exact))
            return SheetMismatchDetail(config, questions, exact);

        for (var index = 0; index < ordered.Count; index++)
            ordered[index].SortOrder = index + 1;
        return null;
    }

    // Bốn đáp án → đúng/sai → điền số, khớp thứ tự phần trên phiếu.
    private static List<ExamQuestion> OrderQuestionsForSheet(IReadOnlyList<ExamQuestion> questions, OmrTemplateConfig config)
    {
        if (config.Sections.Count == 0)
            return questions.OrderBy(item => item.SortOrder).ThenBy(item => item.Id).ToList();

        int Rank(ExamQuestion question) => question.Type switch
        {
            QuestionType.SingleChoice when question.Options.Count == 4 => 0,
            QuestionType.TrueFalse => 1,
            QuestionType.FillNumber => 2,
            _ => question.Options.Count == 4 ? 0 : 9
        };

        return questions
            .OrderBy(Rank)
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToList();
    }

    private static string SheetMismatchDetail(
        OmrTemplateConfig config,
        IReadOnlyList<ExamQuestion> questions,
        bool exact)
    {
        var needBon = config.Sections.Where(item => item.Kind == OmrSectionKinds.BonDapAn).Sum(item => item.ItemCount);
        var needTf = config.Sections.Where(item => item.Kind == OmrSectionKinds.DungSai).Sum(item => item.ItemCount);
        var needNum = config.Sections.Where(item => item.Kind == OmrSectionKinds.DienSo).Sum(item => item.ItemCount);
        var haveBon = questions.Count(item => item.Type == QuestionType.SingleChoice && item.Options.Count == 4);
        var haveTf = questions.Count(item => item.Type == QuestionType.TrueFalse);
        var haveNum = questions.Count(item => item.Type == QuestionType.FillNumber);

        if (exact && questions.Count != config.QuestionCount)
            return $"Mẫu phiếu cần {config.QuestionCount} câu ( {needBon} bốn đáp án, {needTf} đúng/sai, {needNum} điền số ), đề đang có {questions.Count} câu ( {haveBon} bốn đáp án, {haveTf} đúng/sai, {haveNum} điền số ). Hãy Ném vào đề theo mẫu hoặc chọn mẫu khác.";

        if (config.Sections.Count == 0)
            return Messages.ExamSheetMismatch;

        return $"Đề chưa khớp mẫu phiếu. Cần: {needBon} bốn đáp án, {needTf} đúng/sai, {needNum} điền số. Đang có: {haveBon} bốn đáp án, {haveTf} đúng/sai, {haveNum} điền số.";
    }

    private static void Prepare(SaveExamRequest request)
    {
        request.Questions ??= [];
        request.ParticipantUserIds ??= [];
    }

    public async Task<OperationResult<IReadOnlyList<ExamVariantItem>>> ListVariantsAsync(long examId, Actor actor, CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<IReadOnlyList<ExamVariantItem>>.NotFound(Messages.ExamNotFound);
        if (await TeachingGuard.DenyAsync(_unitOfWork, actor.UserId, exam.SubjectId, ct) is { } denied)
            return OperationResult<IReadOnlyList<ExamVariantItem>>.Forbidden(denied);

        await _unitOfWork.Exams.EnsureLegacyVariantAsync(exam, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        var rows = await _unitOfWork.Exams.ListVariantsAsync(examId, ct);
        return OperationResult<IReadOnlyList<ExamVariantItem>>.Ok(rows
            .Select(item => new ExamVariantItem(item.Id, item.Code, item.SortOrder, item.CreatedAtUtc))
            .ToList());
    }

    // Sinh lại toàn bộ mã đề đảo. Có phiếu đã chấm thì không cho đổi.
    public async Task<OperationResult<IReadOnlyList<ExamVariantItem>>> GenerateVariantsAsync(
        long examId,
        GenerateExamVariantsRequest request,
        Actor actor,
        CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<IReadOnlyList<ExamVariantItem>>.NotFound(Messages.ExamNotFound);
        if (await TeachingGuard.DenyAsync(_unitOfWork, actor.UserId, exam.SubjectId, ct) is { } denied)
            return OperationResult<IReadOnlyList<ExamVariantItem>>.Forbidden(denied);
        if (exam.Questions.Count == 0)
            return OperationResult<IReadOnlyList<ExamVariantItem>>.Invalid(Messages.ExamHasNoQuestions);
        if (!ExamVariantCodes.TryParse(request.Codes, request.Count, out var codes, out var parseError))
            return OperationResult<IReadOnlyList<ExamVariantItem>>.Invalid(parseError ?? Messages.ExamVariantCodesInvalid);

        var batches = (await _unitOfWork.Omr.ListBatchesAsync(ct)).Where(item => item.ExamId == examId).ToList();
        if (batches.Any(item => item.ProcessedSheets > 0 || item.TotalSheets > 0))
            return OperationResult<IReadOnlyList<ExamVariantItem>>.Invalid(Messages.ExamVariantsLocked);

        var now = clock.UtcNow;
        var variants = codes
            .Select((code, index) => ExamVariantLayout.Build(exam, code, index + 1, codes.Count, now))
            .ToList();
        await _unitOfWork.Exams.ReplaceVariantsAsync(exam, variants, ct);
        await _unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.GenerateExamVariants,
            AuditCodes.Exam,
            () => exam.Id,
            string.Join(',', codes),
            actor.IpAddress,
            ct);

        var rows = await _unitOfWork.Exams.ListVariantsAsync(examId, ct);
        return OperationResult<IReadOnlyList<ExamVariantItem>>.Ok(rows
            .Select(item => new ExamVariantItem(item.Id, item.Code, item.SortOrder, item.CreatedAtUtc))
            .ToList());
    }

    public async Task<OperationResult<byte[]>> PrintVariantPaperAsync(long examId, string code, Actor actor, CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<byte[]>.NotFound(Messages.ExamNotFound);
        if (await TeachingGuard.DenyAsync(_unitOfWork, actor.UserId, exam.SubjectId, ct) is { } denied)
            return OperationResult<byte[]>.Forbidden(denied);

        await _unitOfWork.Exams.EnsureLegacyVariantAsync(exam, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        var variant = await _unitOfWork.Exams.FindVariantByCodeAsync(examId, code.Trim(), ct);
        if (variant is null)
            return OperationResult<byte[]>.NotFound(Messages.ExamVariantNotFound);

        return OperationResult<byte[]>.Ok(papers.Pdf(exam, variant));
    }

    // In đề: dùng mã đầu tiên, chưa có mã thì tạo mã gốc từ Id đề.
    public async Task<OperationResult<(byte[] Pdf, string Code)>> PrintPaperAsync(long examId, Actor actor, CancellationToken ct)
    {
        var exam = await _unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<(byte[] Pdf, string Code)>.NotFound(Messages.ExamNotFound);
        if (await TeachingGuard.DenyAsync(_unitOfWork, actor.UserId, exam.SubjectId, ct) is { } denied)
            return OperationResult<(byte[] Pdf, string Code)>.Forbidden(denied);
        if (exam.Questions.Count == 0)
            return OperationResult<(byte[] Pdf, string Code)>.Invalid(Messages.ExamHasNoQuestions);

        await _unitOfWork.Exams.EnsureLegacyVariantAsync(exam, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        var variants = await _unitOfWork.Exams.ListVariantsAsync(examId, ct);
        var variant = variants.OrderBy(item => item.SortOrder).FirstOrDefault();
        if (variant is null)
            return OperationResult<(byte[] Pdf, string Code)>.NotFound(Messages.ExamVariantNotFound);

        return OperationResult<(byte[] Pdf, string Code)>.Ok((papers.Pdf(exam, variant), variant.Code));
    }
}
