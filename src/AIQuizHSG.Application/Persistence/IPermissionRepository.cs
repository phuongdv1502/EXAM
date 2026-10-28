using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Danh mục quyền và quyền của từng vai trò.
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
}
