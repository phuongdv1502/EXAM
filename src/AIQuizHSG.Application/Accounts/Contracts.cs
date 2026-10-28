using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Accounts;

public sealed class CreateStudentRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public sealed class UpdateStudentRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public sealed record StudentAccount(long Id, string Username, string FullName, string? Email, UserStatus Status);
