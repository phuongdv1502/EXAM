using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Mappers;

// Đổi tài khoản sang hồ sơ trả về cho client. Không đưa mật khẩu ra ngoài.
public static class UserMapper
{
    public static UserProfile ToProfile(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.Email,
        user.UserRoles.Select(x => x.Role.Code).OrderBy(x => x).ToArray());
}
