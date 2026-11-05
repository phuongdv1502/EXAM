using AIQuizHSG.Application.Omr;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations;

// Bổ sung kiểu phiếu và ba mẫu đang dùng. Database production đã có dữ liệu nên seed C# dễ bị bỏ qua.
[DbContext(typeof(AiQuizDbContext))]
[Migration("20261106021000_SeedMissingOmrSheets")]
public class SeedMissingOmrSheets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        InsertStyle(migrationBuilder, "NhieuPhan", "Phiếu Bộ", "Phiếu Bộ Giáo dục: phần I bốn đáp án, phần II đúng/sai, phần III điền số.", 4);
        InsertStyle(migrationBuilder, "PhieuThi", "Phiếu thi", "Phiếu trắc nghiệm có số báo danh, mã đề và chữ ký cán bộ coi thi. Số cột theo số câu.", 5);
        InsertStyle(migrationBuilder, "KiemTra", "Phiếu kiểm tra", "Phiếu A4: phần I bốn đáp án theo cột 10 câu, phần II đúng/sai. Đặt số câu khi tạo mẫu.", 6);
        InsertStyle(migrationBuilder, "TuLuan", "Phiếu có tự luận", "Phần trắc nghiệm, đúng/sai, điền số, ô điểm tự luận và phần giấy kẻ.", 7);

        if (OmrTemplateConfig.TryFromCheck([("BonDapAn", 32), ("DungSai", 2)], out var check))
            InsertTemplate(migrationBuilder, "PHIEU-KIEM-TRA", check.ToJson());
        if (OmrTemplateConfig.TryFromParts([("BonDapAn", 40), ("DungSai", 8), ("DienSo", 6)], out var ministry))
            InsertTemplate(migrationBuilder, "PHIEU-BO", ministry.ToJson());
        if (OmrTemplateConfig.TryFromEssay([("BonDapAn", 8), ("DungSai", 4), ("DienSo", 2)], out var essay))
            InsertTemplate(migrationBuilder, "PHIEU-TU-LUAN", essay.ToJson());
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE t FROM Omr_Templates t
            WHERE t.Code IN ('PHIEU-KIEM-TRA', 'PHIEU-BO', 'PHIEU-TU-LUAN') AND t.Version = 1
              AND NOT EXISTS (SELECT 1 FROM Omr_Import_Batches b WHERE b.TemplateId = t.Id);
            DELETE FROM Omr_Form_Styles
            WHERE Code IN ('NhieuPhan', 'PhieuThi', 'KiemTra', 'TuLuan');
            """);
    }

    private static void InsertStyle(MigrationBuilder migrationBuilder, string code, string name, string description, int sort)
    {
        migrationBuilder.Sql($"""
            INSERT INTO Omr_Form_Styles (Code, Name, Description, SortOrder, IsActive)
            SELECT '{Sql(code)}', '{Sql(name)}', '{Sql(description)}', {sort}, 1
            FROM DUAL
            WHERE NOT EXISTS (SELECT 1 FROM Omr_Form_Styles WHERE Code = '{Sql(code)}');
            """);
    }

    // Gắn mẫu cho quản trị đầu tiên. Không có tài khoản quản trị thì bỏ qua, không làm hỏng migration.
    private static void InsertTemplate(MigrationBuilder migrationBuilder, string code, string json)
    {
        migrationBuilder.Sql($"""
            INSERT INTO Omr_Templates (Code, Version, ConfigJson, IsActive, CreatedAtUtc, CreatedById)
            SELECT '{Sql(code)}', 1, '{Sql(json)}', 1, UTC_TIMESTAMP(6), admin.Id
            FROM (
                SELECT u.Id
                FROM Users u
                INNER JOIN User_Roles ur ON ur.UserId = u.Id
                INNER JOIN Roles r ON r.Id = ur.RoleId
                WHERE r.Code = 'ADMIN'
                ORDER BY u.Id
                LIMIT 1
            ) admin
            WHERE NOT EXISTS (
                SELECT 1 FROM Omr_Templates t WHERE t.Code = '{Sql(code)}' AND t.Version = 1
            );
            """);
    }

    private static string Sql(string value) => value.Replace("\\", "\\\\").Replace("'", "''");
}
