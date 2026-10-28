using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Accounts;

// Quản trị tạo và khóa học sinh. Giáo viên không tạo được tài khoản quản trị.
public sealed class AccountService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwords,
    IClock clock,
    IValidator<CreateStudentRequest> createValidator,
    IValidator<UpdateStudentRequest> updateValidator)
{
    public async Task<IReadOnlyList<StudentAccount>> ListAsync(CancellationToken ct)
    {
        var users = await unitOfWork.Users.ListByRoleAsync(RoleCodes.Student, ct);
        return users.Select(AccountMapper.ToStudent).ToArray();
    }

    public async Task<OperationResult<StudentAccount>> CreateAsync(CreateStudentRequest request, Actor actor, CancellationToken ct)
    {
        if (await createValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<StudentAccount>.Invalid(inputError);

        var username = request.Username.Trim();
        if (await unitOfWork.Users.FindByUsernameAsync(username, ct) is not null)
            return OperationResult<StudentAccount>.Invalid(Messages.UsernameTaken);

        var role = await unitOfWork.Roles.FindByCodeAsync(RoleCodes.Student, ct);
        if (role is null)
            return OperationResult<StudentAccount>.Invalid(Messages.StudentRoleMissing);

        var user = new User
        {
            Username = username,
            FullName = request.FullName.Trim(),
            Email = CleanEmail(request.Email),
            PasswordHash = passwords.Hash(request.Password),
            Status = UserStatus.Active,
            CreatedAtUtc = clock.UtcNow,
            UserRoles = [new UserRole { RoleId = role.Id, Role = role }]
        };
        await unitOfWork.Users.AddAsync(user, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateStudent, AuditCodes.User, () => user.Id, null, actor.IpAddress, ct);
        return OperationResult<StudentAccount>.Ok(AccountMapper.ToStudent(user));
    }

    public async Task<OperationResult<StudentAccount>> UpdateAsync(long id, UpdateStudentRequest request, Actor actor, CancellationToken ct)
    {
        if (await updateValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<StudentAccount>.Invalid(inputError);

        var user = await FindStudentAsync(id, ct);
        if (user is null)
            return OperationResult<StudentAccount>.NotFound(Messages.StudentNotFound);

        user.FullName = request.FullName.Trim();
        user.Email = CleanEmail(request.Email);
        // Để trống mật khẩu thì giữ mật khẩu cũ.
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = passwords.Hash(request.Password);

        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateStudent, AuditCodes.User, () => user.Id, null, actor.IpAddress, ct);
        return OperationResult<StudentAccount>.Ok(AccountMapper.ToStudent(user));
    }

    public Task<OperationResult<StudentAccount>> LockAsync(long id, Actor actor, CancellationToken ct) =>
        SetStatusAsync(id, UserStatus.Locked, AuditCodes.LockStudent, actor, ct);

    public Task<OperationResult<StudentAccount>> UnlockAsync(long id, Actor actor, CancellationToken ct) =>
        SetStatusAsync(id, UserStatus.Active, AuditCodes.UnlockStudent, actor, ct);

    private async Task<OperationResult<StudentAccount>> SetStatusAsync(
        long id,
        UserStatus status,
        string action,
        Actor actor,
        CancellationToken ct)
    {
        var user = await FindStudentAsync(id, ct);
        if (user is null)
            return OperationResult<StudentAccount>.NotFound(Messages.StudentNotFound);

        // Khóa hoặc mở lại đúng trạng thái thì không ghi thêm nhật ký.
        if (user.Status == status)
            return OperationResult<StudentAccount>.Ok(AccountMapper.ToStudent(user));

        user.Status = status;
        await unitOfWork.SaveWithAuditAsync(actor.UserId, action, AuditCodes.User, () => user.Id, status.ToString(), actor.IpAddress, ct);
        return OperationResult<StudentAccount>.Ok(AccountMapper.ToStudent(user));
    }

    private async Task<User?> FindStudentAsync(long id, CancellationToken ct)
    {
        var user = await unitOfWork.Users.FindForUpdateAsync(id, ct);
        if (user is null || user.UserRoles.All(link => link.Role.Code != RoleCodes.Student))
            return null;
        return user;
    }

    private static string? CleanEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim();
}
