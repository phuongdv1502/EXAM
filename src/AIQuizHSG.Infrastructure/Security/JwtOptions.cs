namespace AIQuizHSG.Infrastructure.Security;

// Đọc từ mục Jwt trong appsettings. Token mặc định hết hạn sau 8 giờ.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AIQuizHSG";
    public string Audience { get; set; } = "AIQuizHSG";
    public int ExpirationMinutes { get; set; } = 480;
}
