using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Organization;

// Gán quyền theo vai trò và quyền ngoại lệ từng người. Tài khoản quản trị không bị ngoại lệ thu hẹp.
public sealed partial class PermissionAdminService : TimedOrganizationService, IPermissionAdminService
{
    private readonly IValidator<ReplacePermissionsRequest> permissionValidator;
    private readonly IValidator<ReplaceUserRolesRequest> roleValidator;
    private readonly IValidator<ReplaceUserPermissionsRequest> grantValidator;

    public PermissionAdminService(
        IUnitOfWork unitOfWork,
        IClock clock,
        ISchoolAccess access,
        IValidator<ReplacePermissionsRequest> permissionValidator,
        IValidator<ReplaceUserRolesRequest> roleValidator,
        IValidator<ReplaceUserPermissionsRequest> grantValidator) : base(unitOfWork, clock, access)
    {
        this.permissionValidator = permissionValidator;
        this.roleValidator = roleValidator;
        this.grantValidator = grantValidator;
    }

    public async Task<OperationResult<IReadOnlyList<PermissionResponse>>> ListAsync(string? module, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Forbidden(denied);
        var items = await _unitOfWork.Permissions.ListAsync(module, ct);
        return OperationResult<IReadOnlyList<PermissionResponse>>.Ok(items.Select(ToPermission).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<string>>> MineAsync(long userId, CancellationToken ct)
    {
        var user = await _unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user is null)
            return OperationResult<IReadOnlyList<string>>.NotFound(Messages.AccountInactive);
        if (user.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin))
        {
            var all = await _unitOfWork.Permissions.ListAsync(null, ct);
            return OperationResult<IReadOnlyList<string>>.Ok(all.Where(item => item.IsActive).Select(item => item.Code).ToArray());
        }

        var codes = await _unitOfWork.Permissions.ListCodesForUserAsync(userId, ct);
        return OperationResult<IReadOnlyList<string>>.Ok(codes);
    }

    public async Task<OperationResult<IReadOnlyList<PermissionResponse>>> ListForRoleAsync(long roleId, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Forbidden(denied);
        if (await _unitOfWork.Roles.FindAsync(roleId, ct) is null)
            return OperationResult<IReadOnlyList<PermissionResponse>>.NotFound(Messages.RoleNotFound);
        var items = await _unitOfWork.Permissions.ListForRoleAsync(roleId, ct);
        return OperationResult<IReadOnlyList<PermissionResponse>>.Ok(items.Select(ToPermission).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<PermissionResponse>>> ReplaceRoleAsync(long roleId, ReplacePermissionsRequest request, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "permission.assign", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Forbidden(denied);
        if (await permissionValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Invalid(error);
        if (await _unitOfWork.Roles.FindAsync(roleId, ct) is null)
            return OperationResult<IReadOnlyList<PermissionResponse>>.NotFound(Messages.RoleNotFound);

        var ids = request.PermissionIds.Distinct().ToArray();
        var found = await _unitOfWork.Permissions.ListByIdsAsync(ids, ct);
        if (found.Count != ids.Length)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Invalid(Messages.PermissionNotFound);

        var before = await _unitOfWork.Permissions.ListForRoleAsync(roleId, ct);
        await _unitOfWork.Permissions.ReplaceRoleAsync(roleId, ids, actor.UserId, clock.UtcNow, ct);
        var after = string.Join(",", found.Select(item => item.Code).OrderBy(code => code));
        var previous = string.Join(",", before.Select(item => item.Code).OrderBy(code => code));
        await _unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.ReplaceRolePermissions,
            AuditCodes.Permission,
            () => roleId,
            previous + " -> " + after,
            actor.IpAddress,
            ct);
        return OperationResult<IReadOnlyList<PermissionResponse>>.Ok(found.Select(ToPermission).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<RoleResponse>>> ListRolesAsync(Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<RoleResponse>>.Forbidden(denied);
        var roles = await _unitOfWork.Roles.ListAsync(ct);
        return OperationResult<IReadOnlyList<RoleResponse>>.Ok(roles.Select(role => new RoleResponse(role.Id, role.Code, role.Name)).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<RoleResponse>>> ListUserRolesAsync(long userId, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<RoleResponse>>.Forbidden(denied);
        var user = await _unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user is null)
            return OperationResult<IReadOnlyList<RoleResponse>>.NotFound(Messages.AccountInactive);
        return OperationResult<IReadOnlyList<RoleResponse>>.Ok(user.UserRoles.Select(link => new RoleResponse(link.Role.Id, link.Role.Code, link.Role.Name)).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<RoleResponse>>> ReplaceUserRolesAsync(long userId, ReplaceUserRolesRequest request, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.manage", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<RoleResponse>>.Forbidden(denied);
        if (await roleValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<IReadOnlyList<RoleResponse>>.Invalid(error);

        var user = await _unitOfWork.Users.FindForUpdateAsync(userId, ct);
        if (user is null)
            return OperationResult<IReadOnlyList<RoleResponse>>.NotFound(Messages.AccountInactive);

        var roles = await _unitOfWork.Roles.ListAsync(ct);
        var selected = roles.Where(role => request.RoleIds.Contains(role.Id)).ToArray();
        if (selected.Length != request.RoleIds.Distinct().Count())
            return OperationResult<IReadOnlyList<RoleResponse>>.Invalid(Messages.RoleNotFound);

        // Người không phải quản trị hệ thống không được trao vai trò đó.
        var actorUser = await _unitOfWork.Users.FindByIdAsync(actor.UserId, ct);
        var actorIsAdmin = actorUser?.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin) == true;
        if (!actorIsAdmin && selected.Any(role => role.Code == RoleCodes.Admin))
            return OperationResult<IReadOnlyList<RoleResponse>>.Invalid(Messages.CannotGrantAdmin);

        // Không để quản trị tự gỡ hết quyền quản trị của chính mình.
        if (user.Id == actor.UserId
            && user.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin)
            && selected.All(role => role.Code != RoleCodes.Admin))
            return OperationResult<IReadOnlyList<RoleResponse>>.Invalid(Messages.CannotDropOwnAdmin);

        user.UserRoles.Clear();
        // Danh sách vai trò không được theo dõi, nên chỉ ghi mã vai trò.
        foreach (var role in selected)
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        await _unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.ReplaceUserRoles,
            AuditCodes.User,
            () => user.Id,
            string.Join(",", selected.Select(role => role.Code)),
            actor.IpAddress,
            ct);
        return OperationResult<IReadOnlyList<RoleResponse>>.Ok(selected.Select(role => new RoleResponse(role.Id, role.Code, role.Name)).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<UserPermissionGrantResponse>>> ListGrantsAsync(long userId, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Forbidden(denied);
        if (await _unitOfWork.Users.FindByIdAsync(userId, ct) is null)
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.NotFound(Messages.AccountNotFound);
        var grants = await _unitOfWork.Permissions.ListGrantsAsync(userId, ct);
        return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Ok(grants.Select(ToGrant).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<UserPermissionGrantResponse>>> ReplaceUserGrantsAsync(
        long userId,
        ReplaceUserPermissionsRequest request,
        Actor actor,
        CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "permission.assign", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Forbidden(denied);
        if (await grantValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Invalid(error);

        var user = await _unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user is null)
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.NotFound(Messages.AccountNotFound);

        // Quản trị hệ thống đi qua mọi quyền, nên thêm hoặc cấm không đổi gì.
        if (user.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin))
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Invalid(Messages.AdminIgnoresGrants);

        var ids = request.Grants.Select(item => item.PermissionId).ToArray();
        if (ids.Distinct().Count() != ids.Length)
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Invalid(Messages.PermissionDuplicate);

        var found = await _unitOfWork.Permissions.ListByIdsAsync(ids, ct);
        if (found.Count != ids.Length)
            return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Invalid(Messages.PermissionNotFound);

        var before = await _unitOfWork.Permissions.ListGrantsAsync(userId, ct);
        var grants = request.Grants
            .Select(item => new UserGrant(item.PermissionId, Enum.Parse<PermissionEffect>(item.Effect)))
            .ToArray();
        await _unitOfWork.Permissions.ReplaceUserGrantsAsync(userId, grants, actor.UserId, clock.UtcNow, ct);
        var response = grants
            .Select(grant =>
            {
                var permission = found.First(item => item.Id == grant.PermissionId);
                return new UserPermissionGrantResponse(permission.Id, permission.Code, permission.Name, permission.Module, grant.Effect);
            })
            .OrderBy(item => item.Module)
            .ThenBy(item => item.Code)
            .ToArray();
        var previous = string.Join(",", before.Select(item => item.Permission.Code + ":" + item.Effect).OrderBy(text => text));
        var after = string.Join(",", response.Select(item => item.Code + ":" + item.Effect));
        await _unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.ReplaceUserPermissions,
            AuditCodes.Permission,
            () => userId,
            previous + " -> " + after,
            actor.IpAddress,
            ct);
        return OperationResult<IReadOnlyList<UserPermissionGrantResponse>>.Ok(response);
    }

    private static UserPermissionGrantResponse ToGrant(UserPermission item) =>
        new(item.PermissionId, item.Permission.Code, item.Permission.Name, item.Permission.Module, item.Effect);

    private static PermissionResponse ToPermission(Permission item) =>
        new(item.Id, item.Code, item.Name, item.Module, item.Description);
}
