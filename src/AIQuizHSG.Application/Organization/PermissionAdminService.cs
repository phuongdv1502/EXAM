using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Organization;

public interface IPermissionAdminService
{
    Task<OperationResult<IReadOnlyList<PermissionResponse>>> ListAsync(string? module, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<string>>> MineAsync(long userId, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<PermissionResponse>>> ListForRoleAsync(long roleId, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<PermissionResponse>>> ReplaceRoleAsync(long roleId, ReplacePermissionsRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<RoleResponse>>> ListRolesAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<RoleResponse>>> ListUserRolesAsync(long userId, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<RoleResponse>>> ReplaceUserRolesAsync(long userId, ReplaceUserRolesRequest request, Actor actor, CancellationToken ct);
}

// Gán quyền theo vai trò và trả quyền hiệu lực. Không có bảng quyền riêng từng người.
public sealed partial class PermissionAdminService : TimedOrganizationService, IPermissionAdminService
{
    private readonly IValidator<ReplacePermissionsRequest> permissionValidator;
    private readonly IValidator<ReplaceUserRolesRequest> roleValidator;

    public PermissionAdminService(
        IUnitOfWork unitOfWork,
        IClock clock,
        ISchoolAccess access,
        IValidator<ReplacePermissionsRequest> permissionValidator,
        IValidator<ReplaceUserRolesRequest> roleValidator) : base(unitOfWork, clock, access)
    {
        this.permissionValidator = permissionValidator;
        this.roleValidator = roleValidator;
    }

    public async Task<OperationResult<IReadOnlyList<PermissionResponse>>> ListAsync(string? module, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Forbidden(denied);
        var items = await unitOfWork.Permissions.ListAsync(module, ct);
        return OperationResult<IReadOnlyList<PermissionResponse>>.Ok(items.Select(ToPermission).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<string>>> MineAsync(long userId, CancellationToken ct)
    {
        var user = await unitOfWork.Users.FindByIdAsync(userId, ct);
        if (user is null)
            return OperationResult<IReadOnlyList<string>>.NotFound(Messages.AccountInactive);
        if (user.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin))
        {
            var all = await unitOfWork.Permissions.ListAsync(null, ct);
            return OperationResult<IReadOnlyList<string>>.Ok(all.Where(item => item.IsActive).Select(item => item.Code).ToArray());
        }

        var codes = await unitOfWork.Permissions.ListCodesForUserAsync(userId, ct);
        return OperationResult<IReadOnlyList<string>>.Ok(codes);
    }

    public async Task<OperationResult<IReadOnlyList<PermissionResponse>>> ListForRoleAsync(long roleId, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Forbidden(denied);
        if (await unitOfWork.Roles.FindAsync(roleId, ct) is null)
            return OperationResult<IReadOnlyList<PermissionResponse>>.NotFound(Messages.RoleNotFound);
        var items = await unitOfWork.Permissions.ListForRoleAsync(roleId, ct);
        return OperationResult<IReadOnlyList<PermissionResponse>>.Ok(items.Select(ToPermission).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<PermissionResponse>>> ReplaceRoleAsync(long roleId, ReplacePermissionsRequest request, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "permission.assign", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Forbidden(denied);
        if (await permissionValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Invalid(error);
        if (await unitOfWork.Roles.FindAsync(roleId, ct) is null)
            return OperationResult<IReadOnlyList<PermissionResponse>>.NotFound(Messages.RoleNotFound);

        var ids = request.PermissionIds.Distinct().ToArray();
        var found = await unitOfWork.Permissions.ListByIdsAsync(ids, ct);
        if (found.Count != ids.Length)
            return OperationResult<IReadOnlyList<PermissionResponse>>.Invalid(Messages.PermissionNotFound);

        var before = await unitOfWork.Permissions.ListForRoleAsync(roleId, ct);
        await unitOfWork.Permissions.ReplaceRoleAsync(roleId, ids, actor.UserId, clock.UtcNow, ct);
        var after = string.Join(",", found.Select(item => item.Code).OrderBy(code => code));
        var previous = string.Join(",", before.Select(item => item.Code).OrderBy(code => code));
        await unitOfWork.SaveWithAuditAsync(
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
        var roles = await unitOfWork.Roles.ListAsync(ct);
        return OperationResult<IReadOnlyList<RoleResponse>>.Ok(roles.Select(role => new RoleResponse(role.Id, role.Code, role.Name)).ToArray());
    }

    public async Task<OperationResult<IReadOnlyList<RoleResponse>>> ListUserRolesAsync(long userId, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "role.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<RoleResponse>>.Forbidden(denied);
        var user = await unitOfWork.Users.FindByIdAsync(userId, ct);
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

        var user = await unitOfWork.Users.FindForUpdateAsync(userId, ct);
        if (user is null)
            return OperationResult<IReadOnlyList<RoleResponse>>.NotFound(Messages.AccountInactive);

        var roles = await unitOfWork.Roles.ListAsync(ct);
        var selected = roles.Where(role => request.RoleIds.Contains(role.Id)).ToArray();
        if (selected.Length != request.RoleIds.Distinct().Count())
            return OperationResult<IReadOnlyList<RoleResponse>>.Invalid(Messages.RoleNotFound);

        // Không để quản trị tự gỡ hết quyền quản trị của chính mình.
        if (user.Id == actor.UserId
            && user.UserRoles.Any(link => link.Role.Code == RoleCodes.Admin)
            && selected.All(role => role.Code != RoleCodes.Admin))
            return OperationResult<IReadOnlyList<RoleResponse>>.Invalid(Messages.CannotDropOwnAdmin);

        user.UserRoles.Clear();
        foreach (var role in selected)
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, Role = role });

        await unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.ReplaceUserRoles,
            AuditCodes.User,
            () => user.Id,
            string.Join(",", selected.Select(role => role.Code)),
            actor.IpAddress,
            ct);
        return OperationResult<IReadOnlyList<RoleResponse>>.Ok(selected.Select(role => new RoleResponse(role.Id, role.Code, role.Name)).ToArray());
    }

    private static PermissionResponse ToPermission(Permission item) =>
        new(item.Id, item.Code, item.Name, item.Module, item.Description);
}
