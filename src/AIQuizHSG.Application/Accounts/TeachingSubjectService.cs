using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Organization;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Accounts;

// Gán nhiều môn cho một giáo viên, và trả môn người đang đăng nhập được phép tạo nội dung.
public sealed class TeachingSubjectService : OrganizationService, ITeachingSubjectService
{
    public TeachingSubjectService(IUnitOfWork unitOfWork, ISchoolAccess access) : base(unitOfWork, access)
    {
    }

    public async Task<IReadOnlyList<SubjectResponse>> ListMineAsync(Actor actor, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.FindByIdAsync(actor.UserId, ct);
        if (user?.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin) == true)
            return await ActiveCatalogAsync(ct);

        var taught = await _unitOfWork.TeacherSubjects.ListAsync(actor.UserId, ct);
        return taught.Where(subject => subject.IsActive).Select(CatalogMapper.ToSubject).ToArray();
    }

    public async Task<OperationResult<IReadOnlyList<SubjectResponse>>> ListCatalogAsync(Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "user.update", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<SubjectResponse>>.Forbidden(denied);
        return OperationResult<IReadOnlyList<SubjectResponse>>.Ok(await ActiveCatalogAsync(ct));
    }

    public async Task<OperationResult<IReadOnlyList<SubjectResponse>>> ListForUserAsync(long userId, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "user.update", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<SubjectResponse>>.Forbidden(denied);
        var taught = await _unitOfWork.TeacherSubjects.ListAsync(userId, ct);
        return OperationResult<IReadOnlyList<SubjectResponse>>.Ok(taught.Select(CatalogMapper.ToSubject).ToArray());
    }

    // Ghi đè môn giảng dạy. Bỏ chọn môn thì giáo viên không còn tạo nội dung môn đó.
    public async Task<OperationResult<IReadOnlyList<SubjectResponse>>> ReplaceAsync(
        long userId,
        IReadOnlyCollection<long> subjectIds,
        Actor actor,
        CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "user.update", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<SubjectResponse>>.Forbidden(denied);

        var user = await _unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user is null)
            return OperationResult<IReadOnlyList<SubjectResponse>>.NotFound(Messages.AccountNotFound);
        if (user.UserRoles.All(link => link.Role.Code != RoleCodes.Teacher))
            return OperationResult<IReadOnlyList<SubjectResponse>>.Invalid(Messages.TeachingSubjectsNeedTeacher);

        var ids = subjectIds.Distinct().ToArray();
        var catalog = await _unitOfWork.Subjects.ListAsync(ct);
        var chosen = catalog.Where(subject => ids.Contains(subject.Id) && subject.IsActive).ToList();
        if (chosen.Count != ids.Length)
            return OperationResult<IReadOnlyList<SubjectResponse>>.Invalid(Messages.TeachingSubjectMissing);

        await _unitOfWork.TeacherSubjects.ReplaceAsync(userId, ids, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.ReplaceTeachingSubjects, AuditCodes.User, () => userId, ids.Length.ToString(), actor.IpAddress, ct);
        return OperationResult<IReadOnlyList<SubjectResponse>>.Ok(chosen.OrderBy(subject => subject.Name).Select(CatalogMapper.ToSubject).ToArray());
    }

    private async Task<IReadOnlyList<SubjectResponse>> ActiveCatalogAsync(CancellationToken ct)
    {
        var subjects = await _unitOfWork.Subjects.ListAsync(ct);
        return subjects.Where(subject => subject.IsActive).OrderBy(subject => subject.Name).Select(CatalogMapper.ToSubject).ToArray();
    }
}
