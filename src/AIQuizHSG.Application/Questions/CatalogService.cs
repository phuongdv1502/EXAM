using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using FluentValidation;

namespace AIQuizHSG.Application.Questions;

public interface ICatalogService
{
    Task<IReadOnlyList<SubjectResponse>> ListSubjectsAsync(CancellationToken ct);
    Task<OperationResult<SubjectResponse>> CreateSubjectAsync(SaveSubjectRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<SubjectResponse>> UpdateSubjectAsync(long id, SaveSubjectRequest request, Actor actor, CancellationToken ct);
    Task<IReadOnlyList<TopicResponse>> ListTopicsAsync(long? subjectId, CancellationToken ct);
    Task<OperationResult<TopicResponse>> CreateTopicAsync(SaveTopicRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<TopicResponse>> UpdateTopicAsync(long id, SaveTopicRequest request, Actor actor, CancellationToken ct);
}

// Môn học và chuyên đề. Chuyên đề có thể lồng nhau nhưng không được tạo vòng cha-con.
public sealed partial class CatalogService : BaseService, ICatalogService
{
    private readonly IValidator<SaveSubjectRequest> subjectValidator;
    private readonly IValidator<SaveTopicRequest> topicValidator;

    public CatalogService(
        IUnitOfWork unitOfWork,
        IValidator<SaveSubjectRequest> subjectValidator,
        IValidator<SaveTopicRequest> topicValidator) : base(unitOfWork)
    {
        this.subjectValidator = subjectValidator;
        this.topicValidator = topicValidator;
    }

    public async Task<IReadOnlyList<SubjectResponse>> ListSubjectsAsync(CancellationToken ct)
    {
        var subjects = await unitOfWork.Subjects.ListAsync(ct);
        return subjects
            .OrderBy(subject => subject.Name)
            .Select(CatalogMapper.ToSubject)
            .ToArray();
    }

    public async Task<OperationResult<SubjectResponse>> CreateSubjectAsync(SaveSubjectRequest request, Actor actor, CancellationToken ct)
    {
        if (await subjectValidator.FirstErrorAsync(request, ct) is { } subjectError)
            return OperationResult<SubjectResponse>.Invalid(subjectError);

        // Mã môn viết hoa và không được trùng.
        var code = CatalogMapper.NormalizeCode(request.Code);
        if (await unitOfWork.Subjects.FindByCodeAsync(code, ct) is not null)
            return OperationResult<SubjectResponse>.Invalid(Messages.SubjectCodeExists);

        var subject = CatalogMapper.CreateSubject(request);
        await unitOfWork.Subjects.AddAsync(subject, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateSubject, AuditCodes.Subject, () => subject.Id, subject.Code, actor.IpAddress, ct);
        return OperationResult<SubjectResponse>.Ok(CatalogMapper.ToSubject(subject));
    }

    public async Task<OperationResult<SubjectResponse>> UpdateSubjectAsync(long id, SaveSubjectRequest request, Actor actor, CancellationToken ct)
    {
        var subject = await unitOfWork.Subjects.FindAsync(id, ct);
        if (subject is null)
            return OperationResult<SubjectResponse>.NotFound(Messages.SubjectNotFound);

        if (await subjectValidator.FirstErrorAsync(request, ct) is { } subjectError)
            return OperationResult<SubjectResponse>.Invalid(subjectError);

        var code = CatalogMapper.NormalizeCode(request.Code);
        // Cho phép giữ nguyên mã của chính môn này, chỉ cấm trùng với môn khác.
        var existing = await unitOfWork.Subjects.FindByCodeAsync(code, ct);
        if (existing is not null && existing.Id != id)
            return OperationResult<SubjectResponse>.Invalid(Messages.SubjectCodeExists);

        CatalogMapper.ApplySubject(subject, request);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateSubject, AuditCodes.Subject, () => subject.Id, subject.Code, actor.IpAddress, ct);
        return OperationResult<SubjectResponse>.Ok(CatalogMapper.ToSubject(subject));
    }

    public async Task<IReadOnlyList<TopicResponse>> ListTopicsAsync(long? subjectId, CancellationToken ct)
    {
        var topics = await unitOfWork.Topics.ListAsync(subjectId, ct);
        return topics
            .OrderBy(topic => topic.SortOrder)
            .ThenBy(topic => topic.Name)
            .Select(CatalogMapper.ToTopic)
            .ToArray();
    }

    public async Task<OperationResult<TopicResponse>> CreateTopicAsync(SaveTopicRequest request, Actor actor, CancellationToken ct)
    {
        if (await topicValidator.FirstErrorAsync(request, ct) is { } topicError)
            return OperationResult<TopicResponse>.Invalid(topicError);

        var subject = await unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null || !subject.IsActive)
            return OperationResult<TopicResponse>.Invalid(Messages.SubjectInactive);

        var parentError = await ValidateParentAsync(null, request.SubjectId, request.ParentTopicId, ct);
        if (parentError is not null)
            return OperationResult<TopicResponse>.Invalid(parentError);

        var topic = CatalogMapper.CreateTopic(request);
        await unitOfWork.Topics.AddAsync(topic, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateTopic, AuditCodes.Topic, () => topic.Id, topic.Name, actor.IpAddress, ct);
        return OperationResult<TopicResponse>.Ok(CatalogMapper.ToTopic(topic));
    }

    public async Task<OperationResult<TopicResponse>> UpdateTopicAsync(long id, SaveTopicRequest request, Actor actor, CancellationToken ct)
    {
        var topic = await unitOfWork.Topics.FindAsync(id, ct);
        if (topic is null)
            return OperationResult<TopicResponse>.NotFound(Messages.TopicNotFound);

        if (await topicValidator.FirstErrorAsync(request, ct) is { } topicError)
            return OperationResult<TopicResponse>.Invalid(topicError);

        var subject = await unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null)
            return OperationResult<TopicResponse>.Invalid(Messages.SubjectMissing);
        var parentError = await ValidateParentAsync(id, request.SubjectId, request.ParentTopicId, ct);
        if (parentError is not null)
            return OperationResult<TopicResponse>.Invalid(parentError);

        CatalogMapper.ApplyTopic(topic, request);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateTopic, AuditCodes.Topic, () => topic.Id, topic.Name, actor.IpAddress, ct);
        return OperationResult<TopicResponse>.Ok(CatalogMapper.ToTopic(topic));
    }

    // Đi ngược lên cha để chặn tự làm cha của mình, khác môn, hoặc cây sâu quá 20 cấp.
    private async Task<string?> ValidateParentAsync(long? topicId, long subjectId, long? parentTopicId, CancellationToken ct)
    {
        var cursor = parentTopicId;
        var guard = 0;
        while (cursor is not null)
        {
            if (topicId is not null && cursor == topicId)
                return Messages.TopicCycle;
            var parent = await unitOfWork.Topics.FindAsync(cursor.Value, ct);
            if (parent is null)
                return Messages.ParentTopicMissing;
            if (parent.SubjectId != subjectId)
                return Messages.ParentTopicDifferentSubject;
            cursor = parent.ParentTopicId;
            if (++guard > 20)
                return Messages.TopicTooDeep;
        }

        return null;
    }
}
