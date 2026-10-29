using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOmrFormStyles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Omr_Form_Styles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Omr_Form_Styles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Form_Styles_Code",
                table: "Omr_Form_Styles",
                column: "Code",
                unique: true);

            migrationBuilder.InsertData(
                table: "Omr_Form_Styles",
                columns: new[] { "Code", "Name", "Description", "SortOrder", "IsActive" },
                values: new object[,]
                {
                    { "Khung", "Khung nhiều cột", "Chia câu thành vài cột, mỗi cột một khung.", 1, true },
                    { "MotCot", "Một cột", "Cả phiếu một cột, ô to, dễ tô.", 2, true },
                    { "Luoi", "Lưới ô", "Câu xếp ngang thành lưới, gọn trên một trang.", 3, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Omr_Form_Styles");
        }
    }
}
