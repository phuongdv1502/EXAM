using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class RoleRepository(AiQuizDbContext db) : IRoleRepository
{
    public Task<bool> AnyAsync(CancellationToken ct) => db.Roles.AnyAsync(ct);

    public Task<Role?> FindByCodeAsync(string code, CancellationToken ct) =>
        db.Roles.FirstOrDefaultAsync(role => role.Code == code, ct);

    public async Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken ct) =>
        await db.Roles.AddRangeAsync(roles, ct);

    public Task<List<Role>> ListAsync(CancellationToken ct) =>
        db.Roles.AsNoTracking().OrderBy(role => role.Code).ToListAsync(ct);

    public Task<Role?> FindAsync(long id, CancellationToken ct) =>
        db.Roles.FirstOrDefaultAsync(role => role.Id == id, ct);
}
