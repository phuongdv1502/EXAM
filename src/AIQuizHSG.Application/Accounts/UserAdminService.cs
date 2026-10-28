using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Organization;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Accounts;

// Tạo và khóa mọi loại tài khoản. Quản trị trường không đụng được tài khoản quản trị hệ thống.
public sealed class UserAdminService : TimedOrganizationService, IUserAdminService
{
    private readonly IPasswordHasher passwords;
    private readonly IValidator<CreateAccountRequest> createValidator;
    private readonly IValidator<UpdateAccountRequest> updateValidator;

    public UserAdminService(
        IUnitOfWork unitOfWork,
        IClock clock,
        ISchoolAccess access,
        IPasswordHasher passwords,
        IValidator<CreateAccountRequest> createValidator,
        IValidator<UpdateAccountRequest> updateValidator) : base(unitOfWork, clock, access)
    {
        this.passwords = passwords;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
    }

    public async Task<OperationResult<AccountDirectory>> ListAsync(Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "user.view", null, ct) is { } denied)
            return OperationResult<AccountDirectory>.Forbidden(denied);

        var users = await _unitOfWork.Users.ListAsync(ct);
        var roles = await _unitOfWork.Roles.ListAsync(ct);
        var actorIsAdmin = await IsAdminAsync(actor.UserId, ct);
        // Form chọn vai trò không đưa vai trò quản trị cho người không phải quản trị hệ thống.
        var assignable = actorIsAdmin ? roles : roles.Where(role => role.Code != RoleCodes.Admin).ToList();
        return OperationResult<AccountDirectory>.Ok(new AccountDirectory(
            users.Select(AccountMapper.ToAccount).ToArray(),
            assignable.Select(role => new AccountRole(role.Id, role.Code, role.Name)).ToArray()));
    }

    public async Task<OperationResult<AccountResponse>> CreateAsync(CreateAccountRequest request, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "user.create", null, ct) is { } denied)
            return OperationResult<AccountResponse>.Forbidden(denied);
        if (await createValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<AccountResponse>.Invalid(inputError);

        var username = request.Username.Trim();
        if (await _unitOfWork.Users.FindByUsernameAsync(username, ct) is not null)
            return OperationResult<AccountResponse>.Invalid(Messages.UsernameTaken);

        var selected = await SelectedRolesAsync(request.RoleIds, ct);
        if (selected is null)
            return OperationResult<AccountResponse>.Invalid(Messages.RoleNotFound);
        if (!await IsAdminAsync(actor.UserId, ct) && selected.Any(role => role.Code == RoleCodes.Admin))
            return OperationResult<AccountResponse>.Invalid(Messages.CannotGrantAdmin);

        var user = new User
        {
            Username = username,
            FullName = request.FullName.Trim(),
            Email = CleanEmail(request.Email),
            PasswordHash = passwords.Hash(request.Password),
            Status = UserStatus.Active,
            CreatedAtUtc = clock.UtcNow,
            // Chỉ gắn mã vai trò. Bản vai trò đọc không theo dõi, gắn cả thực thể sẽ bị ghi thành vai trò mới.
            UserRoles = selected.Select(role => new UserRole { RoleId = role.Id }).ToList()
        };
        await _unitOfWork.Users.AddAsync(user, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateAccount, AuditCodes.User, () => user.Id, string.Join(",", selected.Select(role => role.Code)), actor.IpAddress, ct);
        return OperationResult<AccountResponse>.Ok(new AccountResponse(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Status,
            selected.Select(role => new AccountRole(role.Id, role.Code, role.Name)).OrderBy(role => role.Name).ToArray()));
    }

    public async Task<OperationResult<AccountResponse>> UpdateAsync(long id, UpdateAccountRequest request, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "user.update", null, ct) is { } denied)
            return OperationResult<AccountResponse>.Forbidden(denied);
        if (await updateValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<AccountResponse>.Invalid(inputError);

        var user = await _unitOfWork.Users.FindForUpdateAsync(id, ct);
        if (user is null)
            return OperationResult<AccountResponse>.NotFound(Messages.AccountNotFound);
        if (await ForeignAdminAsync(user, actor, ct) is { } blocked)
            return blocked;

        user.FullName = request.FullName.Trim();
        user.Email = CleanEmail(request.Email);
        // Để trống mật khẩu thì giữ mật khẩu cũ.
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = passwords.Hash(request.Password);

        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateAccount, AuditCodes.User, () => user.Id, null, actor.IpAddress, ct);
        return OperationResult<AccountResponse>.Ok(AccountMapper.ToAccount(user));
    }

    public Task<OperationResult<AccountResponse>> LockAsync(long id, Actor actor, CancellationToken ct) =>
        SetStatusAsync(id, UserStatus.Locked, AuditCodes.LockAccount, actor, ct);

    public Task<OperationResult<AccountResponse>> UnlockAsync(long id, Actor actor, CancellationToken ct) =>
        SetStatusAsync(id, UserStatus.Active, AuditCodes.UnlockAccount, actor, ct);

    private async Task<OperationResult<AccountResponse>> SetStatusAsync(long id, UserStatus status, string action, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "user.disable", null, ct) is { } denied)
            return OperationResult<AccountResponse>.Forbidden(denied);

        var user = await _unitOfWork.Users.FindForUpdateAsync(id, ct);
        if (user is null)
            return OperationResult<AccountResponse>.NotFound(Messages.AccountNotFound);
        // Khóa chính mình thì mất luôn phiên đang dùng để quản trị.
        if (user.Id == actor.UserId)
            return OperationResult<AccountResponse>.Invalid(Messages.CannotLockSelf);
        if (await ForeignAdminAsync(user, actor, ct) is { } blocked)
            return blocked;
        if (user.Status == status)
            return OperationResult<AccountResponse>.Ok(AccountMapper.ToAccount(user));

        user.Status = status;
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, action, AuditCodes.User, () => user.Id, status.ToString(), actor.IpAddress, ct);
        return OperationResult<AccountResponse>.Ok(AccountMapper.ToAccount(user));
    }

    private async Task<OperationResult<AccountResponse>?> ForeignAdminAsync(User user, Actor actor, CancellationToken ct)
    {
        // Quản trị trường sửa được giáo viên và học sinh, không sửa quản trị hệ thống.
        if (user.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin) && !await IsAdminAsync(actor.UserId, ct))
            return OperationResult<AccountResponse>.Forbidden(Messages.CannotTouchAdmin);
        return null;
    }

    private async Task<bool> IsAdminAsync(long userId, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.FindByIdAsync(userId, ct);
        return user?.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin) == true;
    }

    private async Task<Role[]?> SelectedRolesAsync(IReadOnlyCollection<long> roleIds, CancellationToken ct)
    {
        var ids = roleIds.Distinct().ToArray();
        var roles = await _unitOfWork.Roles.ListAsync(ct);
        var selected = roles.Where(role => ids.Contains(role.Id)).ToArray();
        return selected.Length == ids.Length ? selected : null;
    }

    private static string? CleanEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim();
}
