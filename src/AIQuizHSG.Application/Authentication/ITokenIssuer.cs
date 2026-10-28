using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Authentication;

// Phát JWT cho người dùng đã xác thực.
public interface ITokenIssuer
{
    (string Token, DateTime ExpiresAtUtc) Issue(User user);
}
