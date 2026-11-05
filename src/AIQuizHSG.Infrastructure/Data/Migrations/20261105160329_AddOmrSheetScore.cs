using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOmrSheetScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxScore",
                table: "Omr_Scanned_Sheets",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "Omr_Scanned_Sheets",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "Omr_Scanned_Sheets");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Omr_Scanned_Sheets");
        }
    }
}
