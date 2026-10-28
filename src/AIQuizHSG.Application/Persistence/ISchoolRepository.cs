using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Trường, năm học và thành viên trường.
public interface ISchoolRepository
{
    Task<List<School>> ListAsync(CancellationToken ct);
    Task<School?> FindAsync(long id, CancellationToken ct);
    Task<School?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(School school, CancellationToken ct);
    Task<List<SchoolYear>> ListYearsAsync(long schoolId, CancellationToken ct);
    Task<SchoolYear?> FindYearAsync(long id, CancellationToken ct);
    Task<SchoolYear?> FindYearByNameAsync(long schoolId, string name, CancellationToken ct);
    Task AddYearAsync(SchoolYear year, CancellationToken ct);
    Task ClearCurrentAsync(long schoolId, long? exceptYearId, CancellationToken ct);
    Task<bool> HasActiveMembershipAsync(long userId, long schoolId, CancellationToken ct);
    Task<bool> HasActiveRoleAsync(long userId, long schoolId, SchoolRole role, CancellationToken ct);
    Task<SchoolMembership?> FindMembershipAsync(long schoolId, long userId, SchoolRole role, CancellationToken ct);
    Task<List<SchoolMembership>> ListMembershipsAsync(long schoolId, CancellationToken ct);
    Task AddMembershipAsync(SchoolMembership membership, CancellationToken ct);
}
