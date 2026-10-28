namespace AIQuizHSG.Application.Authentication;

// Băm và kiểm tra mật khẩu. Không trả mật khẩu gốc ra ngoài.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
