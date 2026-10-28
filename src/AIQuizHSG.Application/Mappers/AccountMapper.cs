using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Mappers;

// Hồ sơ học sinh trả về cho quản trị. Không kèm mật khẩu.
public static class AccountMapper
{
    public static StudentAccount ToStudent(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Email,
        user.Status);

    public static AccountResponse ToAccount(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Email,
        user.Status,
        user.UserRoles
            .Select(link => new AccountRole(link.Role.Id, link.Role.Code, link.Role.Name))
            .OrderBy(role => role.Name)
            .ToArray());
}
