using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

public readonly record struct UserGrant(long PermissionId, PermissionEffect Effect);

// Danh mục quyền, quyền của vai trò, và quyền ngoại lệ từng người.
public interface IPermissionRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<bool> AnyRoleLinkAsync(CancellationToken ct);
    Task<List<Permission>> ListAsync(string? module, CancellationToken ct);
    Task<List<Permission>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken ct);
    Task<List<string>> ListCodesForUserAsync(long userId, CancellationToken ct);
    Task<List<Permission>> ListForRoleAsync(long roleId, CancellationToken ct);
    Task ReplaceRoleAsync(long roleId, IReadOnlyCollection<long> permissionIds, long? actorId, DateTime now, CancellationToken ct);
    Task AddRoleLinksAsync(IEnumerable<RolePermission> links, CancellationToken ct);
    Task AddMissingRoleLinksAsync(IEnumerable<RolePermission> links, CancellationToken ct);
    Task<List<UserPermission>> ListGrantsAsync(long userId, CancellationToken ct);
    Task ReplaceUserGrantsAsync(long userId, IReadOnlyCollection<UserGrant> grants, long? actorId, DateTime now, CancellationToken ct);
}
