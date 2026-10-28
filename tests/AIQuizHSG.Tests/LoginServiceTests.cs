using System.IdentityModel.Tokens.Jwt;
using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace AIQuizHSG.Tests;

// Kiểm tra đăng nhập: token khi đúng, cùng câu báo khi sai, và từ chối tài khoản khóa.
public class LoginServiceTests
{
    private readonly PasswordHasherService _passwords = new();

    // Mật khẩu đúng thì có token, vai trò ADMIN và một dòng nhật ký Login.
    [Fact]
    public async Task Login_ValidPassword_ReturnsTokenAndRole()
    {
        var user = CreateUser(_passwords.Hash("Admin@123456"));
        var unitOfWork = new MemoryUnitOfWork(user);
        var service = CreateService(unitOfWork);

        var result = await service.LoginAsync(new LoginRequest { Username = " admin ", Password = "Admin@123456" }, "127.0.0.1", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("fake-token", result.Response!.Token);
        Assert.Equal(RoleCodes.Admin, Assert.Single(result.Response.User.Roles));
        Assert.Equal("Login", Assert.Single(unitOfWork.Entries).Action);
    }

    // Sai mật khẩu và không có tài khoản phải trả cùng một câu.
    [Fact]
    public async Task Login_WrongPassword_UsesSameMessageAsUnknownAccount()
    {
        var existing = CreateService(new MemoryUnitOfWork(CreateUser(_passwords.Hash("correct"))));
        var missing = CreateService(new MemoryUnitOfWork(null));

        var wrong = await existing.LoginAsync(new LoginRequest { Username = "admin", Password = "wrong" }, null, CancellationToken.None);
        var unknown = await missing.LoginAsync(new LoginRequest { Username = "missing", Password = "wrong" }, null, CancellationToken.None);

        Assert.False(wrong.Succeeded);
        Assert.False(unknown.Succeeded);
        Assert.Equal(LoginService.InvalidCredentialsMessage, wrong.Message);
        Assert.Equal(wrong.Message, unknown.Message);
    }

    // Đúng mật khẩu nhưng tài khoản khóa thì báo khóa, không cấp token.
    [Fact]
    public async Task Login_LockedAccount_IsRejectedAfterCorrectPassword()
    {
        var user = CreateUser(_passwords.Hash("Admin@123456"), UserStatus.Locked);
        var service = CreateService(new MemoryUnitOfWork(user));

        var result = await service.LoginAsync(new LoginRequest { Username = "admin", Password = "Admin@123456" }, null, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(LoginService.LockedAccountMessage, result.Message);
    }

    // Token không chứa mật khẩu hay chuỗi băm, nhưng có claim role.
    [Fact]
    public void Token_DoesNotContainPassword_AndIncludesRole()
    {
        var passwordHash = _passwords.Hash("secret");
        var user = CreateUser(passwordHash);
        var issuer = new JwtTokenIssuer(Options.Create(new JwtOptions
        {
            SigningKey = new string('k', 32),
            Issuer = "AIQuizHSG",
            Audience = "AIQuizHSG",
            ExpirationMinutes = 30
        }));

        var (token, _) = issuer.Issue(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, claim => claim.Type == "role" && claim.Value == RoleCodes.Admin);
        Assert.DoesNotContain(passwordHash, token, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", token, StringComparison.Ordinal);
    }

    private LoginService CreateService(MemoryUnitOfWork unitOfWork) =>
        new(unitOfWork, _passwords, new TokenIssuerSpy());

    private static User CreateUser(string passwordHash, UserStatus status = UserStatus.Active) =>
        new()
        {
            Id = 7,
            Username = "admin",
            FullName = "Quản trị hệ thống",
            Email = "admin@local",
            PasswordHash = passwordHash,
            Status = status,
            UserRoles = [new UserRole { Role = new Role { Code = RoleCodes.Admin, Name = "Quản trị hệ thống" } }]
        };

    private sealed class MemoryUnitOfWork(User? user) : IUnitOfWork
    {
        public List<(long? UserId, string Action)> Entries { get; } = [];

        public IUserRepository Users => new UserRepositorySpy(user);
        public IRoleRepository Roles => throw new NotSupportedException();
        public ISubjectRepository Subjects => throw new NotSupportedException();
        public ITopicRepository Topics => throw new NotSupportedException();
        public IQuestionRepository Questions => throw new NotSupportedException();
        public IDifficultyLevelRepository DifficultyLevels => throw new NotSupportedException();
        public IExamProfileRepository ExamProfiles => throw new NotSupportedException();
        public ISchoolRepository Schools => throw new NotSupportedException();
        public IGradeLevelRepository GradeLevels => throw new NotSupportedException();
        public IClassroomRepository Classrooms => throw new NotSupportedException();
        public IStudentRecordRepository StudentRecords => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IExamAssignmentRepository ExamAssignments => throw new NotSupportedException();
        public IOmrRepository Omr => throw new NotSupportedException();
        public IExamRepository Exams => throw new NotSupportedException();
        public IAttemptRepository Attempts => throw new NotSupportedException();
        public IAuditLogRepository AuditLogs => new AuditSpy(Entries);

        public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(0);

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct) => action(ct);

        public Task<bool> CanConnectAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class UserRepositorySpy(User? user) : IUserRepository
    {
        public Task<bool> AnyAsync(CancellationToken ct) => Task.FromResult(user is not null);

        public Task<User?> FindByUsernameAsync(string username, CancellationToken ct) =>
            Task.FromResult(user is not null && user.Username == username.Trim() ? user : null);

        public Task<User?> FindByIdAsync(long id, CancellationToken ct) =>
            Task.FromResult(user?.Id == id ? user : null);

        public Task<User?> FindForUpdateAsync(long id, CancellationToken ct) =>
            Task.FromResult(user?.Id == id ? user : null);

        public Task<List<User>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) =>
            Task.FromResult(user is not null && ids.Contains(user.Id) ? new List<User> { user } : []);

        public Task<List<User>> ListByRoleAsync(string roleCode, CancellationToken ct) =>
            Task.FromResult(new List<User>());

        public Task<List<User>> ListAsync(CancellationToken ct) =>
            Task.FromResult(new List<User>());

        public Task AddAsync(User newUser, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class TokenIssuerSpy : ITokenIssuer
    {
        public (string Token, DateTime ExpiresAtUtc) Issue(User user) =>
            ("fake-token", DateTime.UtcNow.AddHours(1));
    }

    private sealed class AuditSpy(List<(long? UserId, string Action)> entries) : IAuditLogRepository
    {
        public void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress) =>
            entries.Add((userId, action));
    }
}
