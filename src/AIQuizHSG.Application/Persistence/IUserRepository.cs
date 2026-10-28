using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Tìm và thêm tài khoản. Không tự lưu; Unit of Work mới gọi SaveChanges.
public interface IUserRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct);
    Task<User?> FindByIdAsync(long id, CancellationToken ct);
    Task<User?> FindForUpdateAsync(long id, CancellationToken ct);
    Task<List<User>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
    Task<List<User>> ListByRoleAsync(string roleCode, CancellationToken ct);
    Task<List<User>> ListAsync(CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}
