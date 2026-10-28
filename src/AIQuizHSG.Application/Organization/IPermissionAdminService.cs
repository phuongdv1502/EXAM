using AIQuizHSG.Application.Common;

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
