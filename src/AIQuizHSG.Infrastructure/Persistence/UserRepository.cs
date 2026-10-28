using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

// Đọc người dùng kèm vai trò. Thêm tài khoản thì theo dõi, còn tra cứu đăng nhập thì không.
public sealed class UserRepository(AiQuizDbContext db) : IUserRepository
{
    public Task<bool> AnyAsync(CancellationToken ct) => db.Users.AnyAsync(ct);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct) =>
        Query().FirstOrDefaultAsync(x => x.Username == username, ct);

    public Task<User?> FindByIdAsync(long id, CancellationToken ct) =>
        Query().FirstOrDefaultAsync(x => x.Id == id, ct);

    // Bản ghi cần sửa hoặc khóa thì phải được theo dõi.
    public Task<User?> FindForUpdateAsync(long id, CancellationToken ct) =>
        db.Users
            .Include(user => user.UserRoles)
            .ThenInclude(link => link.Role)
            .FirstOrDefaultAsync(user => user.Id == id, ct);

    public Task<List<User>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return Task.FromResult(new List<User>());

        return Query().Where(user => ids.Contains(user.Id)).ToListAsync(ct);
    }

    public Task<List<User>> ListByRoleAsync(string roleCode, CancellationToken ct) =>
        Query()
            .Where(user => user.UserRoles.Any(link => link.Role.Code == roleCode))
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Username)
            .ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct) =>
        await db.Users.AddAsync(user, ct);

    private IQueryable<User> Query() =>
        db.Users.AsNoTracking()
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role);
}
