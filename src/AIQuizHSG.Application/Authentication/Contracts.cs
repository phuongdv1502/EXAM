using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Authentication;

// Băm và kiểm tra mật khẩu. Không trả mật khẩu gốc ra ngoài.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

// Phát JWT cho người dùng đã xác thực.
public interface ITokenIssuer
{
    (string Token, DateTime ExpiresAtUtc) Issue(User user);
}

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record UserProfile(
    long Id,
    string Username,
    string FullName,
    string? Email,
    IReadOnlyList<string> Roles);

public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, UserProfile User);

public sealed class LoginResult
{
    public bool Succeeded { get; private init; }
    public string? Message { get; private init; }
    public LoginResponse? Response { get; private init; }

    public static LoginResult Fail(string message) => new() { Succeeded = false, Message = message };

    public static LoginResult Ok(LoginResponse response) => new() { Succeeded = true, Response = response };
}
