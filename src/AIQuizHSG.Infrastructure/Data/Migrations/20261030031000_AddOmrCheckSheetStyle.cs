using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AiQuizDbContext))]
    [Migration("20261030031000_AddOmrCheckSheetStyle")]
    public class AddOmrCheckSheetStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO `Omr_Form_Styles` (`Code`, `Name`, `Description`, `SortOrder`, `IsActive`)
                SELECT 'KiemTra', 'Phiếu kiểm tra', 'Phiếu A4: phần I bốn đáp án theo cột 10 câu, phần II đúng/sai. Đặt số câu khi tạo mẫu.', 6, 1
                WHERE NOT EXISTS (SELECT 1 FROM `Omr_Form_Styles` WHERE `Code` = 'KiemTra');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM `Omr_Form_Styles` WHERE `Code` = 'KiemTra';");
        }
    }
}
