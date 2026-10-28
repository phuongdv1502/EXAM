using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class OmrImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Omr_Templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ConfigJson = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedById = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Omr_Templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Omr_Templates_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "Omr_Import_Batches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ExamId = table.Column<long>(type: "bigint", nullable: false),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedById = table.Column<long>(type: "bigint", nullable: false),
                    ExamCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalSheets = table.Column<int>(type: "int", nullable: false),
                    ProcessedSheets = table.Column<int>(type: "int", nullable: false),
                    FailedSheets = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ApprovedById = table.Column<long>(type: "bigint", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PublishedById = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Omr_Import_Batches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Omr_Import_Batches_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Omr_Import_Batches_Omr_Templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "Omr_Templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Omr_Import_Batches_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "Omr_Scanned_Sheets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(260)", maxLength: 260, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalFilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileHash = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecognizedStudentCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecognizedExamCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedStudentCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedExamCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExamAttemptId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewStatus = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confidence = table.Column<decimal>(type: "decimal(6,4)", precision: 6, scale: 4, nullable: false),
                    FailureReason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewNote = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedById = table.Column<long>(type: "bigint", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Omr_Scanned_Sheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Omr_Scanned_Sheets_Exam_Attempts_ExamAttemptId",
                        column: x => x.ExamAttemptId,
                        principalTable: "Exam_Attempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Omr_Scanned_Sheets_Omr_Import_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Omr_Import_Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Omr_Scanned_Sheets_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateTable(
                name: "Omr_Recognized_Answers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SheetId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    ExamQuestionId = table.Column<long>(type: "bigint", nullable: true),
                    RecognizedOptionId = table.Column<long>(type: "bigint", nullable: true),
                    DetectedMarksJson = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confidence = table.Column<decimal>(type: "decimal(6,4)", precision: 6, scale: 4, nullable: false),
                    RecognitionStatus = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedMarksJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedById = table.Column<long>(type: "bigint", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Omr_Recognized_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Omr_Recognized_Answers_Exam_Options_RecognizedOptionId",
                        column: x => x.RecognizedOptionId,
                        principalTable: "Exam_Options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Omr_Recognized_Answers_Exam_Questions_ExamQuestionId",
                        column: x => x.ExamQuestionId,
                        principalTable: "Exam_Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Omr_Recognized_Answers_Omr_Scanned_Sheets_SheetId",
                        column: x => x.SheetId,
                        principalTable: "Omr_Scanned_Sheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Import_Batches_CreatedById",
                table: "Omr_Import_Batches",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Import_Batches_ExamId",
                table: "Omr_Import_Batches",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Import_Batches_TemplateId",
                table: "Omr_Import_Batches",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Recognized_Answers_ExamQuestionId",
                table: "Omr_Recognized_Answers",
                column: "ExamQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Recognized_Answers_RecognizedOptionId",
                table: "Omr_Recognized_Answers",
                column: "RecognizedOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Recognized_Answers_SheetId_QuestionNumber",
                table: "Omr_Recognized_Answers",
                columns: new[] { "SheetId", "QuestionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Scanned_Sheets_BatchId_FileHash",
                table: "Omr_Scanned_Sheets",
                columns: new[] { "BatchId", "FileHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Scanned_Sheets_ExamAttemptId",
                table: "Omr_Scanned_Sheets",
                column: "ExamAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Scanned_Sheets_ReviewedById",
                table: "Omr_Scanned_Sheets",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Templates_Code_Version",
                table: "Omr_Templates",
                columns: new[] { "Code", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Omr_Templates_CreatedById",
                table: "Omr_Templates",
                column: "CreatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Omr_Recognized_Answers");

            migrationBuilder.DropTable(
                name: "Omr_Scanned_Sheets");

            migrationBuilder.DropTable(
                name: "Omr_Import_Batches");

            migrationBuilder.DropTable(
                name: "Omr_Templates");
        }
    }
}
