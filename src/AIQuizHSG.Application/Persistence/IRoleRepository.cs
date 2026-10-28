using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Vai trò ADMIN, TEACHER, STUDENT. Dùng khi seed và khi gán cho tài khoản.
public interface IRoleRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<Role?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken ct);
    Task<List<Role>> ListAsync(CancellationToken ct);
    Task<Role?> FindAsync(long id, CancellationToken ct);
}
