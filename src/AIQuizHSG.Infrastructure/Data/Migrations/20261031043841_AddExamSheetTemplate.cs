using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSheetTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OmrTemplateId",
                table: "Exams",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_OmrTemplateId",
                table: "Exams",
                column: "OmrTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Omr_Templates_OmrTemplateId",
                table: "Exams",
                column: "OmrTemplateId",
                principalTable: "Omr_Templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Omr_Templates_OmrTemplateId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_Exams_OmrTemplateId",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "OmrTemplateId",
                table: "Exams");
        }
    }
}
