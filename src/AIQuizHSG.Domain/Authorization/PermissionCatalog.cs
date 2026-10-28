using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Authorization;

// Mã quyền ổn định. Tên hiển thị có thể đổi, mã thì không.
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        Item("user.view", "Xem tài khoản", "Tài khoản"),
        Item("user.create", "Tạo tài khoản", "Tài khoản"),
        Item("user.update", "Sửa tài khoản", "Tài khoản"),
        Item("user.disable", "Khóa tài khoản", "Tài khoản"),
        Item("role.view", "Xem vai trò", "Phân quyền"),
        Item("role.manage", "Quản lý vai trò", "Phân quyền"),
        Item("permission.assign", "Gán quyền", "Phân quyền"),
        Item("school.view", "Xem trường", "Trường học"),
        Item("school.manage", "Quản lý trường", "Trường học"),
        Item("class.view", "Xem lớp và học sinh", "Lớp/học sinh"),
        Item("student.create", "Tạo hồ sơ học sinh", "Lớp/học sinh"),
        Item("student.import", "Nhập học sinh từ Excel", "Lớp/học sinh"),
        Item("student.transfer", "Chuyển lớp", "Lớp/học sinh"),
        Item("question.view", "Xem câu hỏi", "Ngân hàng câu hỏi"),
        Item("question.create", "Tạo câu hỏi", "Ngân hàng câu hỏi"),
        Item("question.update", "Sửa câu hỏi", "Ngân hàng câu hỏi"),
        Item("question.approve", "Duyệt câu hỏi", "Ngân hàng câu hỏi"),
        Item("question.delete", "Xóa câu hỏi", "Ngân hàng câu hỏi"),
        Item("exam.create", "Tạo đề", "Đề thi"),
        Item("exam.generate", "Sinh đề", "Đề thi"),
        Item("exam.update", "Sửa đề", "Đề thi"),
        Item("exam.publish", "Phát hành đề", "Đề thi"),
        Item("exam.cancel", "Đóng đề", "Đề thi"),
        Item("exam.assign", "Giao đề", "Đề thi"),
        Item("attempt.view", "Xem lượt làm bài", "Làm bài"),
        Item("attempt.submit", "Nộp bài", "Làm bài"),
        Item("result.view", "Xem kết quả", "Làm bài"),
        Item("result.grade", "Chấm bài", "Làm bài"),
        Item("result.regrade", "Chấm lại", "Làm bài"),
        Item("report.view", "Xem báo cáo", "Báo cáo"),
        Item("report.export", "Xuất báo cáo", "Báo cáo"),
        Item("system.config.view", "Xem cấu hình", "Cấu hình hệ thống"),
        Item("system.config.update", "Sửa cấu hình", "Cấu hình hệ thống")
    ];

    // Quyền mẫu theo vai trò. ADMIN nhận toàn bộ danh mục.
    public static IReadOnlyList<string> ForRole(string roleCode) => roleCode switch
    {
        RoleCodes.Admin => All.Select(item => item.Code).ToArray(),
        RoleCodes.SchoolAdmin =>
        [
            "user.view", "user.create", "user.update", "user.disable",
            "school.view", "school.manage", "class.view",
            "student.create", "student.import", "student.transfer",
            "exam.assign", "report.view", "report.export"
        ],
        RoleCodes.Teacher =>
        [
            "class.view",
            "question.view", "question.create", "question.update", "question.approve",
            "exam.create", "exam.generate", "exam.update", "exam.publish", "exam.assign",
            "attempt.view", "result.view", "report.view"
        ],
        RoleCodes.Student => ["attempt.view", "attempt.submit", "result.view"],
        _ => []
    };

    private static PermissionDefinition Item(string code, string name, string module) => new(code, name, module);
}

public sealed record PermissionDefinition(string Code, string Name, string Module);
