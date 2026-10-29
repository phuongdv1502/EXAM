using System.Security.Cryptography;
using System.Text.Json;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Organization;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Omr;
using ClosedXML.Excel;

namespace AIQuizHSG.Application.Omr;

public sealed class OmrService : TimedOrganizationService, IOmrService
{
    private const int MaxFiles = 20;
    private const int MaxBytes = 12 * 1024 * 1024;
    private const string TemplatePermission = "omr.template.manage";
    private const string BatchPermission = "omr.batch.manage";
    private const string ReviewPermission = "omr.review";
    private const string GradePermission = "omr.grade";
    private const string PublishPermission = "omr.publish";
    private const string ExportPermission = "omr.export";

    private readonly IOmrRecognitionService recognition;
    private readonly IOmrPageSplitter splitter;
    private readonly IOmrFileStore fileStore;
    private readonly IOmrJobQueue queue;
    private readonly IOmrFormRenderer forms;

    public OmrService(
        IUnitOfWork unitOfWork,
        IClock clock,
        ISchoolAccess access,
        IOmrRecognitionService recognition,
        IOmrPageSplitter splitter,
        IOmrFileStore fileStore,
        IOmrJobQueue queue,
        IOmrFormRenderer forms) : base(unitOfWork, clock, access)
    {
        this.recognition = recognition;
        this.splitter = splitter;
        this.fileStore = fileStore;
        this.queue = queue;
        this.forms = forms;
    }

    public async Task<OperationResult<IReadOnlyList<OmrTemplateSummary>>> ListTemplatesAsync(Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, TemplatePermission, ct) is { } denied)
            return OperationResult<IReadOnlyList<OmrTemplateSummary>>.Forbidden(denied);
        var rows = await _unitOfWork.Omr.ListTemplatesAsync(ct);
        return OperationResult<IReadOnlyList<OmrTemplateSummary>>.Ok(rows.Select(ToTemplate).ToList());
    }

    public async Task<OperationResult<OmrTemplateSummary>> CreateTemplateAsync(SaveOmrTemplateRequest request, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, TemplatePermission, ct) is { } denied)
            return OperationResult<OmrTemplateSummary>.Forbidden(denied);

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _unitOfWork.Omr.TemplateCodeExistsAsync(code, request.Version, ct))
            return OperationResult<OmrTemplateSummary>.Invalid(Messages.OmrTemplateCodeExists);

        if (!OmrFormStyles.TryNormalize(request.Style, out var style))
            return OperationResult<OmrTemplateSummary>.Invalid(Messages.OmrTemplateStyleInvalid);

        OmrTemplateConfig config;
        if (string.IsNullOrWhiteSpace(request.ConfigJson))
            config = OmrTemplateConfig.Grid(request.QuestionCount, request.ChoiceCount, style);
        else if (!OmrTemplateConfig.TryParse(request.ConfigJson, out config))
            return OperationResult<OmrTemplateSummary>.Invalid(Messages.OmrTemplateConfigInvalid);

        if (config.QuestionCount != request.QuestionCount || config.ChoiceCount != request.ChoiceCount)
            return OperationResult<OmrTemplateSummary>.Invalid(Messages.OmrTemplateConfigInvalid);

        var template = new OmrTemplate
        {
            Code = code,
            Version = request.Version,
            ConfigJson = config.ToJson(),
            IsActive = request.IsActive,
            CreatedAtUtc = clock.UtcNow,
            CreatedById = actor.UserId
        };
        await _unitOfWork.Omr.AddTemplateAsync(template, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateOmrTemplate, AuditCodes.OmrTemplate, () => template.Id, code, actor.IpAddress, ct);
        return OperationResult<OmrTemplateSummary>.Ok(ToTemplate(template));
    }

    public async Task<OperationResult<IReadOnlyList<OmrFormStyleItem>>> ListFormStylesAsync(Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, TemplatePermission, ct) is { } denied)
            return OperationResult<IReadOnlyList<OmrFormStyleItem>>.Forbidden(denied);
        var rows = await _unitOfWork.Omr.ListFormStylesAsync(ct);
        return OperationResult<IReadOnlyList<OmrFormStyleItem>>.Ok(rows
            .Select(item => new OmrFormStyleItem(item.Id, item.Code, item.Name, item.Description, item.SortOrder))
            .ToList());
    }

    // Xem phiếu trống theo mẫu đã lưu, hoặc theo số câu và kiểu sắp tạo.
    public async Task<OperationResult<byte[]>> PreviewTemplateAsync(long? id, int? questionCount, int? choiceCount, string? style, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, TemplatePermission, ct) is not null && await Deny(actor, BatchPermission, ct) is { } denied)
            return OperationResult<byte[]>.Forbidden(denied);

        OmrTemplateConfig config;
        if (id is long templateId)
        {
            var template = await _unitOfWork.Omr.FindTemplateAsync(templateId, ct);
            if (template is null)
                return OperationResult<byte[]>.NotFound(Messages.OmrTemplateNotFound);
            if (!OmrTemplateConfig.TryParse(template.ConfigJson, out config))
                return OperationResult<byte[]>.Invalid(Messages.OmrTemplateConfigInvalid);
        }
        else if (questionCount is >= 1 and <= 60 && choiceCount is >= 2 and <= 6 && OmrFormStyles.TryNormalize(style, out var code))
        {
            config = OmrTemplateConfig.Grid(questionCount.Value, choiceCount.Value, code);
        }
        else
        {
            return OperationResult<byte[]>.Invalid(Messages.OmrTemplateConfigInvalid);
        }

        return OperationResult<byte[]>.Ok(forms.Png(config));
    }

    public async Task<OperationResult<IReadOnlyList<OmrBatchSummary>>> ListBatchesAsync(Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, BatchPermission, ct) is { } denied)
            return OperationResult<IReadOnlyList<OmrBatchSummary>>.Forbidden(denied);
        var rows = await _unitOfWork.Omr.ListBatchesAsync(ct);
        return OperationResult<IReadOnlyList<OmrBatchSummary>>.Ok(rows.Select(ToBatch).ToList());
    }

    public async Task<OperationResult<OmrBatchSummary>> CreateBatchAsync(CreateOmrBatchRequest request, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, BatchPermission, ct) is { } denied)
            return OperationResult<OmrBatchSummary>.Forbidden(denied);

        var exam = await _unitOfWork.Exams.FindAsync(request.ExamId, ct);
        if (exam is null)
            return OperationResult<OmrBatchSummary>.NotFound(Messages.ExamNotFound);
        if (exam.Status is not ExamStatus.Published and not ExamStatus.Closed)
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrExamNotReady);

        var template = await _unitOfWork.Omr.FindTemplateAsync(request.TemplateId, ct);
        if (template is null)
            return OperationResult<OmrBatchSummary>.NotFound(Messages.OmrTemplateNotFound);
        if (!template.IsActive)
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrTemplateInactive);
        if (!OmrTemplateConfig.TryParse(template.ConfigJson, out var config) || config.QuestionCount != exam.Questions.Count)
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrQuestionMapInvalid);

        if (!Enum.TryParse<OmrSourceType>(request.SourceType, true, out var source))
            source = OmrSourceType.Scanner;

        var batch = new OmrImportBatch
        {
            ExamId = exam.Id,
            Exam = exam,
            TemplateId = template.Id,
            Template = template,
            CreatedById = actor.UserId,
            // Mã đề gắn với đề đã chọn, nên đợt chấm không bắt nhập tay.
            ExamCode = ExamCodeFor(exam),
            SourceType = source,
            Status = OmrBatchStatus.Draft,
            CreatedAtUtc = clock.UtcNow
        };
        await _unitOfWork.Omr.AddBatchAsync(batch, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateOmrBatch, AuditCodes.OmrBatch, () => batch.Id, batch.ExamCode, actor.IpAddress, ct);
        return OperationResult<OmrBatchSummary>.Ok(ToBatch(batch));
    }

    public async Task<OperationResult<OmrBatchSummary>> GetBatchAsync(long id, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, BatchPermission, ct) is { } denied)
            return OperationResult<OmrBatchSummary>.Forbidden(denied);
        var batch = await _unitOfWork.Omr.FindBatchAsync(id, ct);
        if (batch is null)
            return OperationResult<OmrBatchSummary>.NotFound(Messages.OmrBatchNotFound);
        return OperationResult<OmrBatchSummary>.Ok(ToBatch(batch));
    }

    public async Task<OperationResult<UploadOmrSheetsResult>> UploadAsync(
        long batchId,
        string? sourceType,
        IReadOnlyList<OmrUploadFile> files,
        Actor actor,
        CancellationToken ct)
    {
        if (await Deny(actor, BatchPermission, ct) is { } denied)
            return OperationResult<UploadOmrSheetsResult>.Forbidden(denied);
        var batch = await _unitOfWork.Omr.FindBatchAsync(batchId, ct);
        if (batch is null)
            return OperationResult<UploadOmrSheetsResult>.NotFound(Messages.OmrBatchNotFound);
        if (batch.Status is OmrBatchStatus.Approved or OmrBatchStatus.Published)
            return OperationResult<UploadOmrSheetsResult>.Invalid(Messages.OmrBatchLocked);
        if (files.Count == 0)
            return OperationResult<UploadOmrSheetsResult>.Invalid(Messages.OmrFileEmpty);
        if (files.Count > MaxFiles)
            return OperationResult<UploadOmrSheetsResult>.Invalid(Messages.OmrFileTooMany);

        var accepted = new List<(UploadedOmrSheet Item, OmrScannedSheet? Sheet)>();
        var pendingHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (file.Content.Length == 0 || file.Content.Length > MaxBytes)
                return OperationResult<UploadOmrSheetsResult>.Invalid(Messages.OmrFileTooLarge);
            if (!TryContentType(file.Content, file.ContentType, out var contentType))
                return OperationResult<UploadOmrSheetsResult>.Invalid(Messages.OmrFileTypeInvalid);

            var pages = splitter.Split(contentType, file.Content);
            var fileHash = Convert.ToHexString(SHA256.HashData(file.Content));
            foreach (var page in pages)
            {
                var hash = page.Error is null
                    ? Convert.ToHexString(SHA256.HashData(page.Content))
                    : $"{fileHash}:p{page.PageNumber}:failed";
                var existing = pendingHashes.Contains(hash)
                    ? null
                    : await _unitOfWork.Omr.FindSheetByHashAsync(batch.Id, hash, ct);
                if (!pendingHashes.Add(hash) || existing is not null)
                {
                    accepted.Add((new UploadedOmrSheet(existing?.Id ?? 0, file.FileName, page.PageNumber, true, existing?.Status.ToString() ?? OmrSheetStatus.Uploaded.ToString()), null));
                    continue;
                }

                var sheet = new OmrScannedSheet
                {
                    BatchId = batch.Id,
                    PageNumber = page.PageNumber,
                    OriginalFileName = file.FileName,
                    FileHash = hash,
                    CreatedAtUtc = clock.UtcNow,
                    Status = page.Error is null ? OmrSheetStatus.Uploaded : OmrSheetStatus.Failed,
                    FailureReason = page.Error,
                    ContentType = contentType
                };
                if (page.Error is null)
                {
                    var stored = await fileStore.SaveAsync(batch.Id, $"{Guid.NewGuid():N}.img", page.Content, ct);
                    sheet.OriginalFilePath = stored.RelativePath;
                    sheet.ContentType = stored.ContentType;
                }

                await _unitOfWork.Omr.AddSheetAsync(sheet, ct);
                batch.Sheets.Add(sheet);
                accepted.Add((new UploadedOmrSheet(0, file.FileName, page.PageNumber, false, sheet.Status.ToString()), sheet));
            }
        }

        if (Enum.TryParse<OmrSourceType>(sourceType, true, out var source) && source != batch.SourceType)
            batch.SourceType = OmrSourceType.Mixed;
        var existingSheets = await _unitOfWork.Omr.ListSheetStatusesAsync(batch.Id, ct);
        var combined = existingSheets.Concat(accepted.Where(item => item.Sheet is not null).Select(item => item.Sheet!)).ToList();
        Refresh(batch, combined, includeUnsaved: false);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UploadOmrSheets, AuditCodes.OmrBatch, () => batch.Id, accepted.Count.ToString(), actor.IpAddress, ct);

        var result = new List<UploadedOmrSheet>();
        foreach (var (item, sheet) in accepted)
        {
            if (sheet is null)
            {
                result.Add(item);
                continue;
            }

            result.Add(item with { SheetId = sheet.Id, Status = sheet.Status.ToString() });
            if (sheet.Status == OmrSheetStatus.Uploaded)
                queue.Enqueue(sheet.Id);
        }

        return OperationResult<UploadOmrSheetsResult>.Ok(new UploadOmrSheetsResult(batch.Id, result));
    }

    public async Task ProcessSheetAsync(long sheetId, CancellationToken ct)
    {
        var sheet = await _unitOfWork.Omr.FindSheetAsync(sheetId, ct);
        if (sheet is null || sheet.Status is OmrSheetStatus.Graded or OmrSheetStatus.Confirmed)
            return;
        if (sheet.Batch.Status is OmrBatchStatus.Approved or OmrBatchStatus.Published)
            return;

        sheet.Status = OmrSheetStatus.Processing;
        sheet.Batch.Status = OmrBatchStatus.Processing;
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            var bytes = await fileStore.ReadAsync(sheet.OriginalFilePath, ct);
            if (bytes is null)
            {
                await FinishFailed(sheet, "Không đọc được tệp phiếu đã lưu.", ct);
                return;
            }

            if (!OmrTemplateConfig.TryParse(sheet.Batch.Template.ConfigJson, out var config))
            {
                await FinishFailed(sheet, Messages.OmrTemplateConfigInvalid, ct);
                return;
            }

            var read = recognition.Recognize(bytes, config);
            if (read.Error is not null)
            {
                await FinishFailed(sheet, read.Error, ct);
                return;
            }

            ApplyRecognition(sheet, read, config);
            Refresh(sheet.Batch, await _unitOfWork.Omr.ListSheetStatusesAsync(sheet.BatchId, ct), includeUnsaved: false);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            await FinishFailed(sheet, "Xử lý phiếu bị lỗi.", ct);
        }
    }

    public async Task<OperationResult<OmrSheetPage>> ListSheetsAsync(long batchId, string? status, int page, int pageSize, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, BatchPermission, ct) is { } denied)
            return OperationResult<OmrSheetPage>.Forbidden(denied);
        if (await _unitOfWork.Omr.FindBatchAsync(batchId, ct) is null)
            return OperationResult<OmrSheetPage>.NotFound(Messages.OmrBatchNotFound);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await _unitOfWork.Omr.CountSheetsAsync(batchId, status, ct);
        var rows = await _unitOfWork.Omr.ListSheetsAsync(batchId, status, (page - 1) * pageSize, pageSize, ct);
        return OperationResult<OmrSheetPage>.Ok(new OmrSheetPage(rows.Select(ToSheetSummary).ToList(), total));
    }

    public async Task<OperationResult<OmrSheetDetail>> GetSheetAsync(long id, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, ReviewPermission, ct) is { } denied)
            return OperationResult<OmrSheetDetail>.Forbidden(denied);
        var sheet = await _unitOfWork.Omr.FindSheetAsync(id, ct);
        if (sheet is null)
            return OperationResult<OmrSheetDetail>.NotFound(Messages.OmrSheetNotFound);
        return OperationResult<OmrSheetDetail>.Ok(ToDetail(sheet));
    }

    public async Task<OperationResult<(byte[] Content, string ContentType, string FileName)>> OpenFileAsync(long id, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, ReviewPermission, ct) is { } denied)
            return OperationResult<(byte[], string, string)>.Forbidden(denied);
        var sheet = await _unitOfWork.Omr.FindSheetAsync(id, ct);
        if (sheet is null)
            return OperationResult<(byte[], string, string)>.NotFound(Messages.OmrSheetNotFound);
        var bytes = await fileStore.ReadAsync(sheet.OriginalFilePath, ct);
        if (bytes is null)
            return OperationResult<(byte[], string, string)>.NotFound(Messages.OmrSheetNotFound);
        return OperationResult<(byte[], string, string)>.Ok((bytes, sheet.ContentType, sheet.OriginalFileName));
    }

    public async Task<OperationResult<OmrSheetDetail>> ReviewAsync(long id, ReviewOmrSheetRequest request, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, ReviewPermission, ct) is { } denied)
            return OperationResult<OmrSheetDetail>.Forbidden(denied);
        var sheet = await _unitOfWork.Omr.FindSheetAsync(id, ct);
        if (sheet is null)
            return OperationResult<OmrSheetDetail>.NotFound(Messages.OmrSheetNotFound);
        if (sheet.Batch.Status is OmrBatchStatus.Published or OmrBatchStatus.Approved)
            return OperationResult<OmrSheetDetail>.Invalid(Messages.OmrBatchLocked);
        if (sheet.Status is OmrSheetStatus.Uploaded or OmrSheetStatus.Processing)
            return OperationResult<OmrSheetDetail>.Invalid(Messages.OmrSheetNotReady);

        var questions = OrderedQuestions(sheet.Batch.Exam);
        if (request.Answers.Count != sheet.Answers.Count)
            return OperationResult<OmrSheetDetail>.Invalid(Messages.OmrReviewIncomplete);

        var byQuestion = request.Answers.ToDictionary(item => item.ExamQuestionId);
        foreach (var answer in sheet.Answers.OrderBy(item => item.QuestionNumber))
        {
            if (answer.ExamQuestionId is not long questionId || !byQuestion.TryGetValue(questionId, out var picked))
                return OperationResult<OmrSheetDetail>.Invalid(Messages.OmrReviewIncomplete);
            var question = questions.FirstOrDefault(item => item.Id == questionId);
            if (question is null)
                return OperationResult<OmrSheetDetail>.Invalid(Messages.OmrReviewOptionInvalid);
            var allowed = question.Options.Select(option => option.Id).ToHashSet();
            var ids = picked.SelectedOptionIds.Distinct().ToList();
            if (ids.Any(optionId => !allowed.Contains(optionId)))
                return OperationResult<OmrSheetDetail>.Invalid(Messages.OmrReviewOptionInvalid);
            if (question.Type == QuestionType.SingleChoice && ids.Count > 1)
                return OperationResult<OmrSheetDetail>.Invalid(Messages.OmrReviewOptionInvalid);

            // Giữ kết quả máy. Chỉ ghi phần người dùng xác nhận.
            answer.ConfirmedMarksJson = JsonSerializer.Serialize(ids);
            answer.ConfirmedById = actor.UserId;
            answer.ConfirmedAtUtc = clock.UtcNow;
        }

        sheet.ConfirmedStudentCode = request.StudentCode?.Trim();
        sheet.ConfirmedExamCode = request.ExamCode?.Trim().ToUpperInvariant();
        sheet.ReviewNote = request.ReviewNote?.Trim();
        sheet.ReviewStatus = OmrReviewStatus.Confirmed;
        sheet.ReviewedById = actor.UserId;
        sheet.ReviewedAtUtc = clock.UtcNow;
        sheet.Status = OmrSheetStatus.Confirmed;
        sheet.FailureReason = null;
        Refresh(sheet.Batch, await _unitOfWork.Omr.ListSheetStatusesAsync(sheet.BatchId, ct), includeUnsaved: false);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.ReviewOmrSheet, AuditCodes.OmrSheet, () => sheet.Id, sheet.ConfirmedStudentCode, actor.IpAddress, ct);
        return OperationResult<OmrSheetDetail>.Ok(ToDetail(sheet));
    }

    public async Task<OperationResult<OmrSheetSummary>> RetryAsync(long batchId, long sheetId, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, BatchPermission, ct) is { } denied)
            return OperationResult<OmrSheetSummary>.Forbidden(denied);
        var sheet = await _unitOfWork.Omr.FindSheetAsync(sheetId, ct);
        if (sheet is null || sheet.BatchId != batchId)
            return OperationResult<OmrSheetSummary>.NotFound(Messages.OmrSheetNotFound);
        if (sheet.Batch.Status is OmrBatchStatus.Approved or OmrBatchStatus.Published)
            return OperationResult<OmrSheetSummary>.Invalid(Messages.OmrBatchLocked);
        if (sheet.Status is OmrSheetStatus.Graded)
            return OperationResult<OmrSheetSummary>.Invalid(Messages.OmrSheetNotReady);

        sheet.Status = OmrSheetStatus.Uploaded;
        sheet.FailureReason = null;
        sheet.Batch.Status = OmrBatchStatus.Processing;
        await _unitOfWork.SaveChangesAsync(ct);
        queue.Enqueue(sheet.Id);
        return OperationResult<OmrSheetSummary>.Ok(ToSheetSummary(sheet));
    }

    public async Task<OperationResult<OmrBatchSummary>> GradeAsync(long batchId, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, GradePermission, ct) is { } denied)
            return OperationResult<OmrBatchSummary>.Forbidden(denied);
        var batch = await _unitOfWork.Omr.FindBatchAsync(batchId, ct);
        if (batch is null)
            return OperationResult<OmrBatchSummary>.NotFound(Messages.OmrBatchNotFound);
        if (batch.Status is OmrBatchStatus.Approved or OmrBatchStatus.Published)
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrBatchLocked);

        var sheets = await _unitOfWork.Omr.ListSheetsAsync(batchId, null, 0, 500, ct);
        foreach (var summary in sheets.Where(item => OmrRules.CanGrade(item.Status)))
        {
            var sheet = await _unitOfWork.Omr.FindSheetAsync(summary.Id, ct);
            if (sheet is null || !OmrRules.CanGrade(sheet.Status))
                continue;
            await GradeSheetAsync(sheet, ct);
        }

        Refresh(batch, await _unitOfWork.Omr.ListSheetStatusesAsync(batchId, ct), includeUnsaved: false);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.GradeOmrBatch, AuditCodes.OmrBatch, () => batch.Id, batch.Status.ToString(), actor.IpAddress, ct);
        return OperationResult<OmrBatchSummary>.Ok(ToBatch(batch));
    }

    public async Task<OperationResult<OmrBatchSummary>> ApproveAsync(long batchId, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, PublishPermission, ct) is { } denied)
            return OperationResult<OmrBatchSummary>.Forbidden(denied);
        var batch = await _unitOfWork.Omr.FindBatchAsync(batchId, ct);
        if (batch is null)
            return OperationResult<OmrBatchSummary>.NotFound(Messages.OmrBatchNotFound);
        if (batch.Status == OmrBatchStatus.Published)
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrBatchLocked);

        var statuses = (await _unitOfWork.Omr.ListSheetStatusesAsync(batchId, ct)).Select(sheet => sheet.Status).ToList();
        if (!OmrRules.CanApprove(statuses))
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrApproveBlocked);

        batch.Status = OmrBatchStatus.Approved;
        batch.ApprovedAtUtc = clock.UtcNow;
        batch.ApprovedById = actor.UserId;
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.ApproveOmrBatch, AuditCodes.OmrBatch, () => batch.Id, null, actor.IpAddress, ct);
        return OperationResult<OmrBatchSummary>.Ok(ToBatch(batch));
    }

    public async Task<OperationResult<OmrBatchSummary>> PublishAsync(long batchId, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, PublishPermission, ct) is { } denied)
            return OperationResult<OmrBatchSummary>.Forbidden(denied);
        var batch = await _unitOfWork.Omr.FindBatchAsync(batchId, ct);
        if (batch is null)
            return OperationResult<OmrBatchSummary>.NotFound(Messages.OmrBatchNotFound);
        if (batch.Status != OmrBatchStatus.Approved)
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrPublishBlocked);

        var statuses = (await _unitOfWork.Omr.ListSheetStatusesAsync(batchId, ct)).Select(sheet => sheet.Status).ToList();
        if (!OmrRules.CanApprove(statuses))
            return OperationResult<OmrBatchSummary>.Invalid(Messages.OmrPublishBlocked);

        batch.Status = OmrBatchStatus.Published;
        batch.PublishedAtUtc = clock.UtcNow;
        batch.PublishedById = actor.UserId;
        batch.Exam.PublishResults = true;
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.PublishOmrBatch, AuditCodes.OmrBatch, () => batch.Id, null, actor.IpAddress, ct);
        return OperationResult<OmrBatchSummary>.Ok(ToBatch(batch));
    }

    public async Task<OperationResult<IReadOnlyList<OmrResultRow>>> ResultsAsync(long batchId, Actor actor, CancellationToken ct)
    {
        if (await Deny(actor, ExportPermission, ct) is { } denied)
            return OperationResult<IReadOnlyList<OmrResultRow>>.Forbidden(denied);
        var batch = await _unitOfWork.Omr.FindBatchAsync(batchId, ct);
        if (batch is null)
            return OperationResult<IReadOnlyList<OmrResultRow>>.NotFound(Messages.OmrBatchNotFound);
        var sheets = await _unitOfWork.Omr.ListSheetsAsync(batchId, null, 0, 500, ct);
        var rows = new List<OmrResultRow>();
        foreach (var sheet in sheets)
            rows.Add(await ToResultAsync(sheet, ct));
        return OperationResult<IReadOnlyList<OmrResultRow>>.Ok(rows);
    }

    public async Task<OperationResult<byte[]>> ExportAsync(long batchId, Actor actor, CancellationToken ct)
    {
        var results = await ResultsAsync(batchId, actor, ct);
        if (results.Status != OperationStatus.Ok || results.Value is null)
        {
            return results.Status switch
            {
                OperationStatus.NotFound => OperationResult<byte[]>.NotFound(results.Message ?? Messages.OmrBatchNotFound),
                OperationStatus.Forbidden => OperationResult<byte[]>.Forbidden(results.Message ?? Messages.PermissionDenied),
                _ => OperationResult<byte[]>.Invalid(results.Message ?? Messages.OmrBatchNotFound)
            };
        }

        return OperationResult<byte[]>.Ok(ToExcel(results.Value));
    }

    private async Task GradeSheetAsync(OmrScannedSheet sheet, CancellationToken ct)
    {
        var studentCode = (sheet.ConfirmedStudentCode ?? sheet.RecognizedStudentCode)?.Trim();
        var examCode = (sheet.ConfirmedExamCode ?? sheet.RecognizedExamCode)?.Trim();
        if (string.IsNullOrWhiteSpace(studentCode))
        {
            Hold(sheet, Messages.OmrStudentMissing);
            return;
        }

        if (!string.Equals(examCode, sheet.Batch.ExamCode, StringComparison.OrdinalIgnoreCase))
        {
            Hold(sheet, Messages.OmrExamCodeMismatch);
            return;
        }

        if (await _unitOfWork.Omr.HasOtherGradedStudentAsync(sheet.BatchId, sheet.Id, studentCode, ct))
        {
            Hold(sheet, Messages.OmrStudentDuplicate);
            return;
        }

        var students = await _unitOfWork.StudentRecords.FindAllByCodeAsync(studentCode, ct);
        if (students.Count == 0)
        {
            Hold(sheet, Messages.OmrStudentMissing);
            return;
        }

        if (students.Count > 1)
        {
            Hold(sheet, Messages.OmrStudentAmbiguous);
            return;
        }

        var student = students[0];
        if (student.UserId is not long userId)
        {
            Hold(sheet, Messages.OmrStudentNoAccount);
            return;
        }

        var latest = await _unitOfWork.Attempts.FindLatestAsync(sheet.Batch.ExamId, userId, ct);
        if (latest?.Status == AttemptStatus.InProgress)
        {
            Hold(sheet, Messages.OmrAttemptInProgress);
            return;
        }

        var questions = OrderedQuestions(sheet.Batch.Exam);
        var selections = Selections(sheet, questions);
        if (selections is null)
        {
            Hold(sheet, Messages.OmrSheetNotReady);
            return;
        }

        var attempt = latest ?? new ExamAttempt
        {
            ExamId = sheet.Batch.ExamId,
            UserId = userId,
            AttemptNumber = await _unitOfWork.Attempts.CountAsync(sheet.Batch.ExamId, userId, ct) + 1,
            StartedAtUtc = clock.UtcNow,
            DueAtUtc = clock.UtcNow
        };
        if (latest is null)
            await _unitOfWork.Attempts.AddAsync(attempt, ct);

        foreach (var question in questions)
        {
            var answer = attempt.Answers.FirstOrDefault(item => item.ExamQuestionId == question.Id);
            if (answer is null)
            {
                answer = new AttemptAnswer { ExamQuestionId = question.Id, UpdatedAtUtc = clock.UtcNow };
                attempt.Answers.Add(answer);
            }
            else
            {
                answer.UpdatedAtUtc = clock.UtcNow;
            }

            _unitOfWork.Attempts.ReplaceSelections(answer, selections[question.Id]);
        }

        var scored = ExamGrading.Score(questions, questionId => selections[questionId]);
        attempt.Status = AttemptStatus.Graded;
        attempt.SubmittedAtUtc = clock.UtcNow;
        attempt.Score = scored.Score;
        attempt.MaxScore = scored.MaxScore;
        sheet.ExamAttempt = attempt;
        sheet.Status = OmrSheetStatus.Graded;
        sheet.FailureReason = null;
    }

    private static Dictionary<long, IReadOnlyList<long>>? Selections(OmrScannedSheet sheet, IReadOnlyList<ExamQuestion> questions)
    {
        var map = new Dictionary<long, IReadOnlyList<long>>();
        foreach (var question in questions)
        {
            var row = sheet.Answers.FirstOrDefault(item => item.ExamQuestionId == question.Id);
            if (row is null)
                return null;
            if (sheet.Status == OmrSheetStatus.Confirmed)
            {
                map[question.Id] = ReadIds(row.ConfirmedMarksJson);
                continue;
            }

            if (row.RecognitionStatus == OmrMarkStatus.Ambiguous)
                return null;
            map[question.Id] = row.RecognizedOptionId is long optionId ? [optionId] : [];
        }

        return map;
    }

    private void ApplyRecognition(OmrScannedSheet sheet, OmrRecognitionResult read, OmrTemplateConfig config)
    {
        sheet.RecognizedStudentCode = read.StudentCode;
        sheet.RecognizedExamCode = read.ExamCode;
        var questions = OrderedQuestions(sheet.Batch.Exam);
        var answers = new List<OmrRecognizedAnswer>();
        var needsReview = !read.QrFound
            || !string.Equals(read.ExamCode, sheet.Batch.ExamCode, StringComparison.OrdinalIgnoreCase)
            || questions.Count != config.QuestionCount;
        decimal lowest = 1;

        for (var number = 1; number <= config.QuestionCount; number++)
        {
            var bubbles = read.Bubbles.Where(item => item.QuestionNumber == number).ToList();
            var question = number <= questions.Count ? questions[number - 1] : null;
            var options = question?.Options.OrderBy(option => option.SortOrder).ThenBy(option => option.Id).ToList() ?? [];
            var marks = bubbles.Select(bubble =>
            {
                long? optionId = bubble.ChoiceIndex >= 0 && bubble.ChoiceIndex < options.Count ? options[bubble.ChoiceIndex].Id : null;
                return new { bubble.ChoiceIndex, bubble.Ratio, bubble.Status, optionId };
            }).ToList();
            var marked = marks.Where(item => item.Status == nameof(OmrMarkStatus.Marked)).ToList();
            var ambiguous = marks.Any(item => item.Status == nameof(OmrMarkStatus.Ambiguous)) || marked.Count > 1;
            var status = ambiguous
                ? OmrMarkStatus.Ambiguous
                : marked.Count == 1 ? OmrMarkStatus.Marked : OmrMarkStatus.Blank;
            if (status == OmrMarkStatus.Ambiguous)
                needsReview = true;

            var confidence = status switch
            {
                OmrMarkStatus.Marked => (decimal)marked[0].Ratio,
                OmrMarkStatus.Blank => (decimal)(1 - marks.Select(item => item.Ratio).DefaultIfEmpty(0).Max()),
                _ => 0.4m
            };
            lowest = Math.Min(lowest, confidence);
            answers.Add(new OmrRecognizedAnswer
            {
                QuestionNumber = number,
                ExamQuestionId = question?.Id,
                RecognizedOptionId = status == OmrMarkStatus.Marked ? marked[0].optionId : null,
                DetectedMarksJson = JsonSerializer.Serialize(marks),
                Confidence = confidence,
                RecognitionStatus = status
            });
        }

        _unitOfWork.Omr.ReplaceAnswers(sheet, answers);
        sheet.Confidence = read.QrFound ? lowest : 0;
        sheet.FailureReason = needsReview
            ? !read.QrFound ? "Không đọc được mã học sinh hoặc mã đề."
            : !string.Equals(read.ExamCode, sheet.Batch.ExamCode, StringComparison.OrdinalIgnoreCase) ? Messages.OmrExamCodeMismatch
            : questions.Count != config.QuestionCount ? Messages.OmrQuestionMapInvalid
            : "Có ô tô mơ hồ hoặc tô nhiều đáp án."
            : null;
        sheet.Status = needsReview ? OmrSheetStatus.NeedsReview : OmrSheetStatus.Recognized;
    }

    private async Task FinishFailed(OmrScannedSheet sheet, string reason, CancellationToken ct)
    {
        sheet.Status = OmrSheetStatus.Failed;
        sheet.FailureReason = reason;
        Refresh(sheet.Batch, await _unitOfWork.Omr.ListSheetStatusesAsync(sheet.BatchId, ct), includeUnsaved: false);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static void Hold(OmrScannedSheet sheet, string reason)
    {
        sheet.Status = OmrSheetStatus.NeedsReview;
        sheet.FailureReason = reason;
    }

    private static void Refresh(OmrImportBatch batch, IReadOnlyCollection<OmrScannedSheet> sheets, bool includeUnsaved)
    {
        var rows = includeUnsaved ? batch.Sheets.ToList() : sheets.ToList();
        batch.TotalSheets = rows.Count;
        batch.ProcessedSheets = rows.Count(sheet => sheet.Status is not OmrSheetStatus.Uploaded and not OmrSheetStatus.Processing);
        batch.FailedSheets = rows.Count(sheet => sheet.Status == OmrSheetStatus.Failed);
        if (batch.Status is OmrBatchStatus.Approved or OmrBatchStatus.Published)
            return;
        batch.Status = rows.Any(sheet => sheet.Status is OmrSheetStatus.Uploaded or OmrSheetStatus.Processing)
            ? OmrBatchStatus.Processing
            : rows.Count == 0 ? OmrBatchStatus.Draft : OmrBatchStatus.Reviewing;
    }

    private async Task<OmrResultRow> ToResultAsync(OmrScannedSheet sheet, CancellationToken ct)
    {
        var code = sheet.ConfirmedStudentCode ?? sheet.RecognizedStudentCode;
        string? name = null;
        if (!string.IsNullOrWhiteSpace(code))
        {
            var students = await _unitOfWork.StudentRecords.FindAllByCodeAsync(code, ct);
            if (students.Count == 1)
                name = students[0].FullName;
        }

        var correct = 0;
        var wrong = 0;
        var blank = 0;
        decimal? score = sheet.ExamAttempt?.Score;
        decimal? max = sheet.ExamAttempt?.MaxScore;
        if (sheet.ExamAttempt is null && sheet.Answers.Count > 0 && sheet.Batch.Exam.Questions.Count > 0)
        {
            var questions = OrderedQuestions(sheet.Batch.Exam);
            var selections = Selections(sheet.Status == OmrSheetStatus.Confirmed ? sheet : WithRecognized(sheet), questions);
            if (selections is not null)
            {
                var preview = ExamGrading.Score(questions, questionId => selections[questionId]);
                correct = preview.Correct;
                wrong = preview.Wrong;
                blank = preview.Blank;
            }
        }
        else if (sheet.ExamAttempt is not null)
        {
            var questions = OrderedQuestions(sheet.Batch.Exam);
            var preview = ExamGrading.Score(questions, questionId =>
                sheet.ExamAttempt.Answers.FirstOrDefault(answer => answer.ExamQuestionId == questionId)?.SelectedOptions
                    .Select(option => option.ExamOptionId)
                    .ToArray() ?? []);
            correct = preview.Correct;
            wrong = preview.Wrong;
            blank = preview.Blank;
        }

        return new OmrResultRow(sheet.Id, code, name, sheet.ConfirmedExamCode ?? sheet.RecognizedExamCode, correct, wrong, blank, score, max, sheet.Status.ToString());
    }

    private static OmrScannedSheet WithRecognized(OmrScannedSheet sheet) => sheet;

    private static List<long> ReadIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<long>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<ExamQuestion> OrderedQuestions(Exam exam) =>
        exam.Questions.OrderBy(question => question.SortOrder).ThenBy(question => question.Id).ToList();

    private async Task<string?> Deny(Actor actor, string permission, CancellationToken ct) =>
        await access.DenyAsync(actor.UserId, permission, null, ct);

    private static bool TryContentType(byte[] content, string declared, out string contentType)
    {
        contentType = string.Empty;
        if (content.Length >= 4 && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47)
        {
            contentType = "image/png";
            return true;
        }

        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            contentType = "image/jpeg";
            return true;
        }

        if (content.Length >= 12 && content[0] == (byte)'R' && content[1] == (byte)'I' && content[8] == (byte)'W' && content[9] == (byte)'E')
        {
            contentType = "image/webp";
            return true;
        }

        if (content.Length >= 4 && content[0] == (byte)'%' && content[1] == (byte)'P' && content[2] == (byte)'D' && content[3] == (byte)'F')
        {
            contentType = "application/pdf";
            return true;
        }

        _ = declared;
        return false;
    }

    private static byte[] ToExcel(IReadOnlyList<OmrResultRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("BangDiem");
        string[] headers = ["MaHocSinh", "HoTen", "MaDe", "Dung", "Sai", "BoTrong", "Diem", "ThangDiem", "TrangThai"];
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        for (var row = 0; row < rows.Count; row++)
        {
            var item = rows[row];
            sheet.Cell(row + 2, 1).Value = item.StudentCode ?? "";
            sheet.Cell(row + 2, 2).Value = item.StudentName ?? "";
            sheet.Cell(row + 2, 3).Value = item.ExamCode ?? "";
            sheet.Cell(row + 2, 4).Value = item.Correct;
            sheet.Cell(row + 2, 5).Value = item.Wrong;
            sheet.Cell(row + 2, 6).Value = item.Blank;
            sheet.Cell(row + 2, 7).Value = item.Score;
            sheet.Cell(row + 2, 8).Value = item.MaxScore;
            sheet.Cell(row + 2, 9).Value = item.Status;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static OmrTemplateSummary ToTemplate(OmrTemplate template)
    {
        OmrTemplateConfig.TryParse(template.ConfigJson, out var config);
        return new OmrTemplateSummary(template.Id, template.Code, template.Version, config.QuestionCount, config.ChoiceCount, template.IsActive, config.Style, template.CreatedAtUtc);
    }

    private static OmrBatchSummary ToBatch(OmrImportBatch batch) => new(
        batch.Id,
        batch.ExamId,
        batch.Exam?.Title ?? "",
        batch.TemplateId,
        batch.Template?.Code ?? "",
        batch.ExamCode,
        batch.SourceType.ToString(),
        batch.Status.ToString(),
        batch.TotalSheets,
        batch.ProcessedSheets,
        batch.FailedSheets,
        batch.Sheets.Count(sheet => sheet.Status == OmrSheetStatus.NeedsReview),
        batch.CreatedAtUtc);

    private static OmrSheetSummary ToSheetSummary(OmrScannedSheet sheet) => new(
        sheet.Id,
        sheet.PageNumber,
        sheet.OriginalFileName,
        sheet.ConfirmedStudentCode ?? sheet.RecognizedStudentCode,
        sheet.ConfirmedExamCode ?? sheet.RecognizedExamCode,
        sheet.Status.ToString(),
        sheet.ReviewStatus.ToString(),
        sheet.Confidence,
        sheet.FailureReason,
        sheet.ExamAttemptId);

    private static OmrSheetDetail ToDetail(OmrScannedSheet sheet)
    {
        var questions = OrderedQuestions(sheet.Batch.Exam).ToDictionary(question => question.Id);
        var answers = sheet.Answers.OrderBy(answer => answer.QuestionNumber).Select(answer =>
        {
            questions.TryGetValue(answer.ExamQuestionId ?? 0, out var question);
            var choices = question?.Options.OrderBy(option => option.SortOrder).ThenBy(option => option.Id)
                .Select(option => new OmrChoiceDetail(option.Id, option.ContentSnapshot, option.SortOrder))
                .ToList() ?? [];
            return new OmrAnswerDetail(
                answer.QuestionNumber,
                answer.ExamQuestionId,
                question?.ContentSnapshot,
                question?.Type.ToString(),
                answer.RecognitionStatus.ToString(),
                answer.Confidence,
                answer.RecognizedOptionId,
                ReadIds(answer.ConfirmedMarksJson),
                choices);
        }).ToList();

        return new OmrSheetDetail(
            sheet.Id,
            sheet.BatchId,
            sheet.Status.ToString(),
            sheet.ReviewStatus.ToString(),
            sheet.Confidence,
            sheet.ConfirmedStudentCode ?? sheet.RecognizedStudentCode,
            sheet.ConfirmedExamCode ?? sheet.RecognizedExamCode,
            sheet.Batch.ExamCode,
            sheet.FailureReason,
            sheet.ReviewNote,
            answers,
            sheet.Batch.Status.ToString());
    }

    // Cùng một đề thì cùng một mã in trên phiếu.
    private static string ExamCodeFor(Exam exam) => $"DE{exam.Id}";
}
