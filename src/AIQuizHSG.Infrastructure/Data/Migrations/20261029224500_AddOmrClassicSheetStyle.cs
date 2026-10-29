using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AiQuizDbContext))]
    [Migration("20261029224500_AddOmrClassicSheetStyle")]
    public class AddOmrClassicSheetStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO `Omr_Form_Styles` (`Code`, `Name`, `Description`, `SortOrder`, `IsActive`)
                SELECT 'PhieuThi', 'Phiếu thi', 'Phiếu trắc nghiệm có số báo danh, mã đề và chữ ký cán bộ coi thi. Số cột theo số câu.', 5, 1
                WHERE NOT EXISTS (SELECT 1 FROM `Omr_Form_Styles` WHERE `Code` = 'PhieuThi');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM `Omr_Form_Styles` WHERE `Code` = 'PhieuThi';");
        }
    }
}
