using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Questions;
using FluentValidation;

namespace AIQuizHSG.Application.Questions;

public interface IQuestionService
{
    Task<QuestionPage> SearchAsync(QuestionFilter filter, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> GetAsync(long id, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> CreateAsync(SaveQuestionRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> UpdateAsync(long id, SaveQuestionRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> SubmitAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> ApproveAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> RejectAsync(long id, RejectQuestionRequest request, Actor actor, CancellationToken ct);
    byte[] CreateImportTemplate();
    Task<OperationResult<ImportResult>> ImportAsync(Stream stream, Actor actor, CancellationToken ct);
}

// Ngân hàng câu hỏi: tạo nháp, gửi duyệt, duyệt hoặc từ chối, và nhập từ Excel.
public sealed partial class QuestionService : BaseService, IQuestionService
{
    private readonly IValidator<SaveQuestionRequest> questionValidator;
    private readonly IValidator<RejectQuestionRequest> rejectValidator;

    public QuestionService(
        IUnitOfWork unitOfWork,
        IValidator<SaveQuestionRequest> questionValidator,
        IValidator<RejectQuestionRequest> rejectValidator) : base(unitOfWork)
    {
        this.questionValidator = questionValidator;
        this.rejectValidator = rejectValidator;
    }

    // Lọc theo môn, chuyên đề, mức độ, trạng thái và từ khóa. Mỗi trang tối đa 100 câu.
    public async Task<QuestionPage> SearchAsync(QuestionFilter filter, CancellationToken ct)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);
        var (total, items) = await unitOfWork.Questions.SearchAsync(filter, ct);
        return new QuestionPage(total, items.Select(QuestionMapper.ToSummary).ToArray());
    }

    public async Task<OperationResult<QuestionResponse>> GetAsync(long id, CancellationToken ct)
    {
        var question = await unitOfWork.Questions.FindAsync(id, ct);
        if (question is null)
            return OperationResult<QuestionResponse>.NotFound(Messages.QuestionNotFound);
        return OperationResult<QuestionResponse>.Ok(QuestionMapper.ToResponse(question));
    }

    public async Task<OperationResult<QuestionResponse>> CreateAsync(SaveQuestionRequest request, Actor actor, CancellationToken ct)
    {
        if (await questionValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<QuestionResponse>.Invalid(inputError);

        var topic = await unitOfWork.Topics.FindAsync(request.TopicId, ct);
        if (topic is null || !topic.IsActive)
            return OperationResult<QuestionResponse>.Invalid(Messages.TopicInactive);

        var difficulty = await ResolveDifficultyAsync(request.DifficultyLevelId, assignDefault: true, ct);
        if (difficulty.Error is not null)
            return OperationResult<QuestionResponse>.Invalid(difficulty.Error);

        // Câu tạo tay luôn vào nháp. Chỉ gửi duyệt mới chuyển sang chờ duyệt.
        var question = QuestionMapper.CreateDraft(request, topic, actor, DateTime.UtcNow);
        question.DifficultyLevelId = difficulty.Id;
        await unitOfWork.Questions.AddAsync(question, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateQuestion, AuditCodes.Question, () => question.Id, null, actor.IpAddress, ct);
        return OperationResult<QuestionResponse>.Ok(QuestionMapper.ToResponse(question));
    }

    // Cập nhật nội dung. Câu đã duyệt sẽ bị đưa về chờ duyệt.
    public async Task<OperationResult<QuestionResponse>> UpdateAsync(long id, SaveQuestionRequest request, Actor actor, CancellationToken ct)
    {
        if (await questionValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<QuestionResponse>.Invalid(inputError);

        var question = await unitOfWork.Questions.FindAsync(id, ct);
        if (question is null)
            return OperationResult<QuestionResponse>.NotFound(Messages.QuestionNotFound);
        var topic = await unitOfWork.Topics.FindAsync(request.TopicId, ct);
        if (topic is null || !topic.IsActive)
            return OperationResult<QuestionResponse>.Invalid(Messages.TopicInactive);

        QuestionMapper.ApplyContent(question, request, topic);
        // Không gửi mức mới thì giữ mức cũ, kể cả câu chưa được phân loại.
        if (request.DifficultyLevelId is byte)
        {
            var difficulty = await ResolveDifficultyAsync(request.DifficultyLevelId, assignDefault: false, ct);
            if (difficulty.Error is not null)
                return OperationResult<QuestionResponse>.Invalid(difficulty.Error);
            question.DifficultyLevelId = difficulty.Id;
        }
        // Sửa câu đã duyệt thì đưa lại chờ duyệt và xóa người đã duyệt.
        if (question.Status == QuestionStatus.Approved)
        {
            question.Status = QuestionStatus.PendingReview;
            QuestionMapper.ClearReview(question);
        }

        unitOfWork.Questions.ReplaceOptions(question, QuestionMapper.BuildOptions(request.Options));
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateQuestion, AuditCodes.Question, () => question.Id, question.Status.ToString(), actor.IpAddress, ct);
        return OperationResult<QuestionResponse>.Ok(QuestionMapper.ToResponse(question));
    }

    // Giáo viên gửi câu nháp hoặc câu bị từ chối sang chờ duyệt.
    public async Task<OperationResult<QuestionResponse>> SubmitAsync(long id, Actor actor, CancellationToken ct)
    {
        var question = await unitOfWork.Questions.FindAsync(id, ct);
        if (question is null)
            return OperationResult<QuestionResponse>.NotFound(Messages.QuestionNotFound);
        if (question.Status is not (QuestionStatus.Draft or QuestionStatus.Rejected))
            return OperationResult<QuestionResponse>.Invalid(Messages.SubmitNotAllowed);

        // Kiểm tra lại phương án đã lưu, phòng dữ liệu cũ không còn hợp lệ.
        var optionError = QuestionRules.ValidateOptions(question.Type, QuestionMapper.OptionPairs(question.Options));
        if (optionError is not null)
            return OperationResult<QuestionResponse>.Invalid(optionError);

        QuestionMapper.MarkSubmitted(question);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.SubmitQuestion, AuditCodes.Question, () => question.Id, null, actor.IpAddress, ct);
        return OperationResult<QuestionResponse>.Ok(QuestionMapper.ToResponse(question));
    }

    // Duyệt câu đang chờ. Ghi người duyệt và thời điểm duyệt.
    public async Task<OperationResult<QuestionResponse>> ApproveAsync(long id, Actor actor, CancellationToken ct)
    {
        var question = await unitOfWork.Questions.FindAsync(id, ct);
        if (question is null)
            return OperationResult<QuestionResponse>.NotFound(Messages.QuestionNotFound);
        // Chỉ câu đang chờ duyệt mới được duyệt.
        if (question.Status != QuestionStatus.PendingReview)
            return OperationResult<QuestionResponse>.Invalid(Messages.ApproveNotAllowed);

        QuestionMapper.MarkApproved(question, actor.UserId, DateTime.UtcNow);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.ApproveQuestion, AuditCodes.Question, () => question.Id, null, actor.IpAddress, ct);
        return OperationResult<QuestionResponse>.Ok(QuestionMapper.ToResponse(question));
    }

    // Từ chối câu đang chờ và lưu lý do để người tạo sửa lại.
    public async Task<OperationResult<QuestionResponse>> RejectAsync(long id, RejectQuestionRequest request, Actor actor, CancellationToken ct)
    {
        if (await rejectValidator.FirstErrorAsync(request, ct) is { } reasonError)
            return OperationResult<QuestionResponse>.Invalid(reasonError);

        var question = await unitOfWork.Questions.FindAsync(id, ct);
        if (question is null)
            return OperationResult<QuestionResponse>.NotFound(Messages.QuestionNotFound);
        // Từ chối bắt buộc có lý do và chỉ áp dụng khi câu đang chờ duyệt.
        if (question.Status != QuestionStatus.PendingReview)
            return OperationResult<QuestionResponse>.Invalid(Messages.RejectNotAllowed);

        QuestionMapper.MarkRejected(question, actor.UserId, request.Reason, DateTime.UtcNow);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.RejectQuestion, AuditCodes.Question, () => question.Id, null, actor.IpAddress, ct);
        return OperationResult<QuestionResponse>.Ok(QuestionMapper.ToResponse(question));
    }

    public byte[] CreateImportTemplate() => QuestionExcel.CreateTemplate();

    public async Task<OperationResult<ImportResult>> ImportAsync(Stream stream, Actor actor, CancellationToken ct)
    {
        var read = QuestionExcel.Read(stream);
        if (read.HasFatalError)
            return OperationResult<ImportResult>.Invalid(read.FatalMessage ?? Messages.ExcelInvalid);

        var levels = await unitOfWork.DifficultyLevels.ListActiveAsync(ct);
        // Dòng lỗi bị bỏ qua. Dòng hợp lệ được thêm thẳng vào chờ duyệt, không qua nháp.
        var imported = 0;
        foreach (var row in read.Rows)
        {
            var subject = await unitOfWork.Subjects.FindByCodeAsync(row.SubjectCode, ct);
            if (subject is null || !subject.IsActive)
            {
                read.Errors.Add(new ImportError(row.RowNumber, string.Format(Messages.SubjectNotFoundByCode, row.SubjectCode)));
                continue;
            }

            var topics = await unitOfWork.Topics.ListAsync(subject.Id, ct);
            var topic = topics.FirstOrDefault(item =>
                item.IsActive && string.Equals(item.Name.Trim(), row.TopicName, StringComparison.OrdinalIgnoreCase));
            if (topic is null)
            {
                read.Errors.Add(new ImportError(row.RowNumber, string.Format(Messages.TopicNotFoundInSubject, row.TopicName, row.SubjectCode)));
                continue;
            }

            // Chỉ gán TopicId. Chuyên đề vừa tra là bản không theo dõi, gán navigation sẽ bị EF thêm mới.
            var question = QuestionMapper.CreateImported(row, topic.Id, actor, DateTime.UtcNow);
            if (!string.IsNullOrWhiteSpace(row.DifficultyText))
            {
                var level = levels.FirstOrDefault(item => DifficultyText.Matches(item, row.DifficultyText));
                if (level is null)
                {
                    read.Errors.Add(new ImportError(row.RowNumber, Messages.ExcelDifficultyInvalid));
                    continue;
                }

                question.DifficultyLevelId = level.Id;
            }

            await unitOfWork.Questions.AddAsync(question, ct);
            imported++;
        }

        if (imported > 0)
        {
            await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.ImportQuestions, AuditCodes.Question, () => null, string.Format(Messages.ImportAudit, imported), actor.IpAddress, ct);
        }

        var errors = read.Errors.OrderBy(error => error.Row).ToArray();
        return OperationResult<ImportResult>.Ok(new ImportResult(imported, errors));
    }

    // Câu mới bỏ trống mức thì gán Mức 3. Câu cũ không bị gán khi sửa nếu không gửi mức.
    private async Task<(byte? Id, string? Error)> ResolveDifficultyAsync(byte? requested, bool assignDefault, CancellationToken ct)
    {
        if (requested is null && !assignDefault)
            return (null, null);

        var level = requested is byte id
            ? await unitOfWork.DifficultyLevels.FindAsync(id, ct)
            : await unitOfWork.DifficultyLevels.FindByCodeAsync("3", ct);
        if (level is null || !level.IsActive)
            return (null, requested is null ? Messages.DifficultyCatalogMissing : Messages.DifficultyLevelNotFound);
        return (level.Id, null);
    }
}
