using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOmrMultiPartStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm kiểu phiếu nhiều phần nếu máy đã có ba kiểu cũ.
            migrationBuilder.Sql("""
                INSERT INTO `Omr_Form_Styles` (`Code`, `Name`, `Description`, `SortOrder`, `IsActive`)
                SELECT 'NhieuPhan', 'Nhiều phần', 'Phần bốn đáp án, phần đúng/sai và phần trả lời số trên cùng một phiếu.', 4, 1
                WHERE NOT EXISTS (SELECT 1 FROM `Omr_Form_Styles` WHERE `Code` = 'NhieuPhan');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM `Omr_Form_Styles` WHERE `Code` = 'NhieuPhan';");
        }
    }
}
