using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AIQuizHSG.Infrastructure.Security;

// Băm mật khẩu bằng PasswordHasher của ASP.NET Identity. Không lưu mật khẩu gốc.
public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string passwordHash) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
