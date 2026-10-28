using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Authentication;

// Xác thực đăng nhập rồi phát JWT. Sai mật khẩu và không có tài khoản trả cùng một câu.
public sealed class LoginService(IUnitOfWork unitOfWork, IPasswordHasher passwords, ITokenIssuer tokens)
{
    public const string InvalidCredentialsMessage = Messages.InvalidCredentials;
    public const string LockedAccountMessage = Messages.LockedAccount;

    private static string? _dummyHash;

    public async Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct)
    {
        var username = request.Username.Trim();
        var user = await unitOfWork.Users.FindByUsernameAsync(username, ct);
        if (user is null)
        {
            // Vẫn kiểm tra một hash giả để thời gian trả lời gần với trường hợp sai mật khẩu.
            passwords.Verify(request.Password, DummyHash());
            await WriteFailureAsync(null, username, ipAddress, ct);
            return LoginResult.Fail(InvalidCredentialsMessage);
        }

        if (!passwords.Verify(request.Password, user.PasswordHash))
        {
            await WriteFailureAsync(user.Id, username, ipAddress, ct);
            return LoginResult.Fail(InvalidCredentialsMessage);
        }

        // Chỉ báo khóa sau khi mật khẩu đúng, tránh lộ trạng thái tài khoản khi đoán mật khẩu.
        if (user.Status != UserStatus.Active)
        {
            await unitOfWork.SaveWithAuditAsync(user.Id, AuditCodes.LoginRejected, AuditCodes.User, () => user.Id, Messages.LockedAccount, ipAddress, ct);
            return LoginResult.Fail(LockedAccountMessage);
        }

        var (token, expiresAt) = tokens.Issue(user);
        await unitOfWork.SaveWithAuditAsync(user.Id, AuditCodes.Login, AuditCodes.User, () => user.Id, null, ipAddress, ct);
        return LoginResult.Ok(new LoginResponse(token, expiresAt, UserMapper.ToProfile(user)));
    }

    private Task WriteFailureAsync(long? userId, string username, string? ipAddress, CancellationToken ct) =>
        unitOfWork.SaveWithAuditAsync(userId, AuditCodes.LoginFailed, AuditCodes.User, () => userId, string.Format(Messages.LoginFailedDetail, username), ipAddress, ct);

    private string DummyHash() => _dummyHash ??= passwords.Hash(AuditCodes.TimingProbePassword);
}
