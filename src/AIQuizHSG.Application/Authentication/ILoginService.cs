namespace AIQuizHSG.Application.Authentication;

public interface ILoginService
{
    Task<LoginResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct);
}
