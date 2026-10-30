using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AiQuizDbContext))]
    [Migration("20261030040000_AddOmrEssayAndMinistryNames")]
    public class AddOmrEssayAndMinistryNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE `Omr_Form_Styles`
                SET `Name` = 'Phiếu Bộ',
                    `Description` = 'Phiếu Bộ Giáo dục: phần I bốn đáp án, phần II đúng/sai, phần III điền số.'
                WHERE `Code` = 'NhieuPhan';

                INSERT INTO `Omr_Form_Styles` (`Code`, `Name`, `Description`, `SortOrder`, `IsActive`)
                SELECT 'TuLuan', 'Phiếu có tự luận', 'Phần trắc nghiệm, đúng/sai, điền số, ô điểm tự luận và phần giấy kẻ.', 7, 1
                WHERE NOT EXISTS (SELECT 1 FROM `Omr_Form_Styles` WHERE `Code` = 'TuLuan');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM `Omr_Form_Styles` WHERE `Code` = 'TuLuan';
                UPDATE `Omr_Form_Styles`
                SET `Name` = 'Nhiều phần',
                    `Description` = 'Phần bốn đáp án, phần đúng/sai và phần trả lời số trên cùng một phiếu.'
                WHERE `Code` = 'NhieuPhan';
                """);
        }
    }
}
