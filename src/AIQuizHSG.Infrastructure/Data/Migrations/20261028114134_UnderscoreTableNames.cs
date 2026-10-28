using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIQuizHSG.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnderscoreTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AiAnalysisReports_ExamAttempts_AttemptId",
                table: "AiAnalysisReports");

            migrationBuilder.DropForeignKey(
                name: "FK_AiAnalysisReports_Exams_ExamId",
                table: "AiAnalysisReports");

            migrationBuilder.DropForeignKey(
                name: "FK_AiAnalysisReports_Users_RequestedById",
                table: "AiAnalysisReports");

            migrationBuilder.DropForeignKey(
                name: "FK_AiQuestionRequests_Topics_TopicId",
                table: "AiQuestionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AiQuestionRequests_Users_RequestedById",
                table: "AiQuestionRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AttemptAnswerOptions_AttemptAnswers_AttemptAnswerId_ExamQues~",
                table: "AttemptAnswerOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_AttemptAnswerOptions_ExamOptions_ExamOptionId_ExamQuestionId",
                table: "AttemptAnswerOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_AttemptAnswers_ExamAttempts_AttemptId",
                table: "AttemptAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_AttemptAnswers_ExamQuestions_ExamQuestionId",
                table: "AttemptAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAttempts_Exams_ExamId",
                table: "ExamAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAttempts_Users_UserId",
                table: "ExamAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamDifficultyProfileLevels_DifficultyLevels_DifficultyLevel~",
                table: "ExamDifficultyProfileLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamDifficultyProfileLevels_ExamDifficultyProfiles_ProfileId",
                table: "ExamDifficultyProfileLevels");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamDifficultyProfiles_Users_CreatedById",
                table: "ExamDifficultyProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamOptions_ExamQuestions_ExamQuestionId",
                table: "ExamOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamOptions_QuestionOptions_SourceOptionId",
                table: "ExamOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamParticipants_Exams_ExamId",
                table: "ExamParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamParticipants_Users_UserId",
                table: "ExamParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamQuestions_Exams_ExamId",
                table: "ExamQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamQuestions_Questions_SourceQuestionId",
                table: "ExamQuestions");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_ExamDifficultyProfiles_DifficultyProfileId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestionOptions_Questions_QuestionId",
                table: "QuestionOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_DifficultyLevels_DifficultyLevelId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_QuestionOptions",
                table: "QuestionOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamQuestions",
                table: "ExamQuestions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamParticipants",
                table: "ExamParticipants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ExamOptions_Id_ExamQuestionId",
                table: "ExamOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamOptions",
                table: "ExamOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamDifficultyProfiles",
                table: "ExamDifficultyProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamDifficultyProfileLevels",
                table: "ExamDifficultyProfileLevels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamAttempts",
                table: "ExamAttempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DifficultyLevels",
                table: "DifficultyLevels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AttemptAnswers_Id_ExamQuestionId",
                table: "AttemptAnswers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttemptAnswers",
                table: "AttemptAnswers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AttemptAnswerOptions",
                table: "AttemptAnswerOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AiQuestionRequests",
                table: "AiQuestionRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AiAnalysisReports",
                table: "AiAnalysisReports");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "User_Roles");

            migrationBuilder.RenameTable(
                name: "QuestionOptions",
                newName: "Question_Options");

            migrationBuilder.RenameTable(
                name: "ExamQuestions",
                newName: "Exam_Questions");

            migrationBuilder.RenameTable(
                name: "ExamParticipants",
                newName: "Exam_Participants");

            migrationBuilder.RenameTable(
                name: "ExamOptions",
                newName: "Exam_Options");

            migrationBuilder.RenameTable(
                name: "ExamDifficultyProfiles",
                newName: "Exam_Difficulty_Profiles");

            migrationBuilder.RenameTable(
                name: "ExamDifficultyProfileLevels",
                newName: "Exam_Difficulty_Profile_Levels");

            migrationBuilder.RenameTable(
                name: "ExamAttempts",
                newName: "Exam_Attempts");

            migrationBuilder.RenameTable(
                name: "DifficultyLevels",
                newName: "Difficulty_Levels");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "Audit_Logs");

            migrationBuilder.RenameTable(
                name: "AttemptAnswers",
                newName: "Attempt_Answers");

            migrationBuilder.RenameTable(
                name: "AttemptAnswerOptions",
                newName: "Attempt_Answer_Options");

            migrationBuilder.RenameTable(
                name: "AiQuestionRequests",
                newName: "Ai_Question_Requests");

            migrationBuilder.RenameTable(
                name: "AiAnalysisReports",
                newName: "Ai_Analysis_Reports");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                table: "User_Roles",
                newName: "IX_User_Roles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_QuestionOptions_QuestionId",
                table: "Question_Options",
                newName: "IX_Question_Options_QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamQuestions_SourceQuestionId",
                table: "Exam_Questions",
                newName: "IX_Exam_Questions_SourceQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamQuestions_ExamId_SourceQuestionId",
                table: "Exam_Questions",
                newName: "IX_Exam_Questions_ExamId_SourceQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamParticipants_UserId",
                table: "Exam_Participants",
                newName: "IX_Exam_Participants_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamParticipants_ExamId_UserId",
                table: "Exam_Participants",
                newName: "IX_Exam_Participants_ExamId_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamOptions_SourceOptionId",
                table: "Exam_Options",
                newName: "IX_Exam_Options_SourceOptionId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamOptions_ExamQuestionId",
                table: "Exam_Options",
                newName: "IX_Exam_Options_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamDifficultyProfiles_CreatedById",
                table: "Exam_Difficulty_Profiles",
                newName: "IX_Exam_Difficulty_Profiles_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_ExamDifficultyProfileLevels_ProfileId_DifficultyLevelId",
                table: "Exam_Difficulty_Profile_Levels",
                newName: "IX_Exam_Difficulty_Profile_Levels_ProfileId_DifficultyLevelId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamDifficultyProfileLevels_DifficultyLevelId",
                table: "Exam_Difficulty_Profile_Levels",
                newName: "IX_Exam_Difficulty_Profile_Levels_DifficultyLevelId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAttempts_UserId",
                table: "Exam_Attempts",
                newName: "IX_Exam_Attempts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ExamAttempts_ExamId_UserId_AttemptNumber",
                table: "Exam_Attempts",
                newName: "IX_Exam_Attempts_ExamId_UserId_AttemptNumber");

            migrationBuilder.RenameIndex(
                name: "IX_DifficultyLevels_Code",
                table: "Difficulty_Levels",
                newName: "IX_Difficulty_Levels_Code");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_UserId",
                table: "Audit_Logs",
                newName: "IX_Audit_Logs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_OccurredAtUtc",
                table: "Audit_Logs",
                newName: "IX_Audit_Logs_OccurredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_AttemptAnswers_ExamQuestionId",
                table: "Attempt_Answers",
                newName: "IX_Attempt_Answers_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_AttemptAnswers_AttemptId_ExamQuestionId",
                table: "Attempt_Answers",
                newName: "IX_Attempt_Answers_AttemptId_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_AttemptAnswerOptions_ExamOptionId_ExamQuestionId",
                table: "Attempt_Answer_Options",
                newName: "IX_Attempt_Answer_Options_ExamOptionId_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_AttemptAnswerOptions_AttemptAnswerId_ExamQuestionId",
                table: "Attempt_Answer_Options",
                newName: "IX_Attempt_Answer_Options_AttemptAnswerId_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_AiQuestionRequests_TopicId",
                table: "Ai_Question_Requests",
                newName: "IX_Ai_Question_Requests_TopicId");

            migrationBuilder.RenameIndex(
                name: "IX_AiQuestionRequests_RequestedById",
                table: "Ai_Question_Requests",
                newName: "IX_Ai_Question_Requests_RequestedById");

            migrationBuilder.RenameIndex(
                name: "IX_AiAnalysisReports_RequestedById",
                table: "Ai_Analysis_Reports",
                newName: "IX_Ai_Analysis_Reports_RequestedById");

            migrationBuilder.RenameIndex(
                name: "IX_AiAnalysisReports_ExamId",
                table: "Ai_Analysis_Reports",
                newName: "IX_Ai_Analysis_Reports_ExamId");

            migrationBuilder.RenameIndex(
                name: "IX_AiAnalysisReports_AttemptId",
                table: "Ai_Analysis_Reports",
                newName: "IX_Ai_Analysis_Reports_AttemptId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User_Roles",
                table: "User_Roles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Question_Options",
                table: "Question_Options",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exam_Questions",
                table: "Exam_Questions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exam_Participants",
                table: "Exam_Participants",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Exam_Options_Id_ExamQuestionId",
                table: "Exam_Options",
                columns: new[] { "Id", "ExamQuestionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exam_Options",
                table: "Exam_Options",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exam_Difficulty_Profiles",
                table: "Exam_Difficulty_Profiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exam_Difficulty_Profile_Levels",
                table: "Exam_Difficulty_Profile_Levels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Exam_Attempts",
                table: "Exam_Attempts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Difficulty_Levels",
                table: "Difficulty_Levels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Audit_Logs",
                table: "Audit_Logs",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Attempt_Answers_Id_ExamQuestionId",
                table: "Attempt_Answers",
                columns: new[] { "Id", "ExamQuestionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Attempt_Answers",
                table: "Attempt_Answers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Attempt_Answer_Options",
                table: "Attempt_Answer_Options",
                columns: new[] { "AttemptAnswerId", "ExamOptionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ai_Question_Requests",
                table: "Ai_Question_Requests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ai_Analysis_Reports",
                table: "Ai_Analysis_Reports",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ai_Analysis_Reports_Exam_Attempts_AttemptId",
                table: "Ai_Analysis_Reports",
                column: "AttemptId",
                principalTable: "Exam_Attempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ai_Analysis_Reports_Exams_ExamId",
                table: "Ai_Analysis_Reports",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ai_Analysis_Reports_Users_RequestedById",
                table: "Ai_Analysis_Reports",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ai_Question_Requests_Topics_TopicId",
                table: "Ai_Question_Requests",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ai_Question_Requests_Users_RequestedById",
                table: "Ai_Question_Requests",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempt_Answer_Options_Attempt_Answers_AttemptAnswerId_ExamQ~",
                table: "Attempt_Answer_Options",
                columns: new[] { "AttemptAnswerId", "ExamQuestionId" },
                principalTable: "Attempt_Answers",
                principalColumns: new[] { "Id", "ExamQuestionId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempt_Answer_Options_Exam_Options_ExamOptionId_ExamQuestio~",
                table: "Attempt_Answer_Options",
                columns: new[] { "ExamOptionId", "ExamQuestionId" },
                principalTable: "Exam_Options",
                principalColumns: new[] { "Id", "ExamQuestionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempt_Answers_Exam_Attempts_AttemptId",
                table: "Attempt_Answers",
                column: "AttemptId",
                principalTable: "Exam_Attempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempt_Answers_Exam_Questions_ExamQuestionId",
                table: "Attempt_Answers",
                column: "ExamQuestionId",
                principalTable: "Exam_Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Audit_Logs_Users_UserId",
                table: "Audit_Logs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Attempts_Exams_ExamId",
                table: "Exam_Attempts",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Attempts_Users_UserId",
                table: "Exam_Attempts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Difficulty_Profile_Levels_Difficulty_Levels_DifficultyL~",
                table: "Exam_Difficulty_Profile_Levels",
                column: "DifficultyLevelId",
                principalTable: "Difficulty_Levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Difficulty_Profile_Levels_Exam_Difficulty_Profiles_Prof~",
                table: "Exam_Difficulty_Profile_Levels",
                column: "ProfileId",
                principalTable: "Exam_Difficulty_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Difficulty_Profiles_Users_CreatedById",
                table: "Exam_Difficulty_Profiles",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Options_Exam_Questions_ExamQuestionId",
                table: "Exam_Options",
                column: "ExamQuestionId",
                principalTable: "Exam_Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Options_Question_Options_SourceOptionId",
                table: "Exam_Options",
                column: "SourceOptionId",
                principalTable: "Question_Options",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Participants_Exams_ExamId",
                table: "Exam_Participants",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Participants_Users_UserId",
                table: "Exam_Participants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Questions_Exams_ExamId",
                table: "Exam_Questions",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Exam_Questions_Questions_SourceQuestionId",
                table: "Exam_Questions",
                column: "SourceQuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_Exam_Difficulty_Profiles_DifficultyProfileId",
                table: "Exams",
                column: "DifficultyProfileId",
                principalTable: "Exam_Difficulty_Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Question_Options_Questions_QuestionId",
                table: "Question_Options",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_Difficulty_Levels_DifficultyLevelId",
                table: "Questions",
                column: "DifficultyLevelId",
                principalTable: "Difficulty_Levels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Roles_Roles_RoleId",
                table: "User_Roles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_User_Roles_Users_UserId",
                table: "User_Roles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ai_Analysis_Reports_Exam_Attempts_AttemptId",
                table: "Ai_Analysis_Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Ai_Analysis_Reports_Exams_ExamId",
                table: "Ai_Analysis_Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Ai_Analysis_Reports_Users_RequestedById",
                table: "Ai_Analysis_Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Ai_Question_Requests_Topics_TopicId",
                table: "Ai_Question_Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Ai_Question_Requests_Users_RequestedById",
                table: "Ai_Question_Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempt_Answer_Options_Attempt_Answers_AttemptAnswerId_ExamQ~",
                table: "Attempt_Answer_Options");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempt_Answer_Options_Exam_Options_ExamOptionId_ExamQuestio~",
                table: "Attempt_Answer_Options");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempt_Answers_Exam_Attempts_AttemptId",
                table: "Attempt_Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempt_Answers_Exam_Questions_ExamQuestionId",
                table: "Attempt_Answers");

            migrationBuilder.DropForeignKey(
                name: "FK_Audit_Logs_Users_UserId",
                table: "Audit_Logs");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Attempts_Exams_ExamId",
                table: "Exam_Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Attempts_Users_UserId",
                table: "Exam_Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Difficulty_Profile_Levels_Difficulty_Levels_DifficultyL~",
                table: "Exam_Difficulty_Profile_Levels");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Difficulty_Profile_Levels_Exam_Difficulty_Profiles_Prof~",
                table: "Exam_Difficulty_Profile_Levels");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Difficulty_Profiles_Users_CreatedById",
                table: "Exam_Difficulty_Profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Options_Exam_Questions_ExamQuestionId",
                table: "Exam_Options");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Options_Question_Options_SourceOptionId",
                table: "Exam_Options");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Participants_Exams_ExamId",
                table: "Exam_Participants");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Participants_Users_UserId",
                table: "Exam_Participants");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Questions_Exams_ExamId",
                table: "Exam_Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Exam_Questions_Questions_SourceQuestionId",
                table: "Exam_Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_Exam_Difficulty_Profiles_DifficultyProfileId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_Question_Options_Questions_QuestionId",
                table: "Question_Options");

            migrationBuilder.DropForeignKey(
                name: "FK_Questions_Difficulty_Levels_DifficultyLevelId",
                table: "Questions");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Roles_Roles_RoleId",
                table: "User_Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_User_Roles_Users_UserId",
                table: "User_Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User_Roles",
                table: "User_Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Question_Options",
                table: "Question_Options");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exam_Questions",
                table: "Exam_Questions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exam_Participants",
                table: "Exam_Participants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Exam_Options_Id_ExamQuestionId",
                table: "Exam_Options");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exam_Options",
                table: "Exam_Options");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exam_Difficulty_Profiles",
                table: "Exam_Difficulty_Profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exam_Difficulty_Profile_Levels",
                table: "Exam_Difficulty_Profile_Levels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Exam_Attempts",
                table: "Exam_Attempts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Difficulty_Levels",
                table: "Difficulty_Levels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Audit_Logs",
                table: "Audit_Logs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Attempt_Answers_Id_ExamQuestionId",
                table: "Attempt_Answers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Attempt_Answers",
                table: "Attempt_Answers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Attempt_Answer_Options",
                table: "Attempt_Answer_Options");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ai_Question_Requests",
                table: "Ai_Question_Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ai_Analysis_Reports",
                table: "Ai_Analysis_Reports");

            migrationBuilder.RenameTable(
                name: "User_Roles",
                newName: "UserRoles");

            migrationBuilder.RenameTable(
                name: "Question_Options",
                newName: "QuestionOptions");

            migrationBuilder.RenameTable(
                name: "Exam_Questions",
                newName: "ExamQuestions");

            migrationBuilder.RenameTable(
                name: "Exam_Participants",
                newName: "ExamParticipants");

            migrationBuilder.RenameTable(
                name: "Exam_Options",
                newName: "ExamOptions");

            migrationBuilder.RenameTable(
                name: "Exam_Difficulty_Profiles",
                newName: "ExamDifficultyProfiles");

            migrationBuilder.RenameTable(
                name: "Exam_Difficulty_Profile_Levels",
                newName: "ExamDifficultyProfileLevels");

            migrationBuilder.RenameTable(
                name: "Exam_Attempts",
                newName: "ExamAttempts");

            migrationBuilder.RenameTable(
                name: "Difficulty_Levels",
                newName: "DifficultyLevels");

            migrationBuilder.RenameTable(
                name: "Audit_Logs",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "Attempt_Answers",
                newName: "AttemptAnswers");

            migrationBuilder.RenameTable(
                name: "Attempt_Answer_Options",
                newName: "AttemptAnswerOptions");

            migrationBuilder.RenameTable(
                name: "Ai_Question_Requests",
                newName: "AiQuestionRequests");

            migrationBuilder.RenameTable(
                name: "Ai_Analysis_Reports",
                newName: "AiAnalysisReports");

            migrationBuilder.RenameIndex(
                name: "IX_User_Roles_RoleId",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_Question_Options_QuestionId",
                table: "QuestionOptions",
                newName: "IX_QuestionOptions_QuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Questions_SourceQuestionId",
                table: "ExamQuestions",
                newName: "IX_ExamQuestions_SourceQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Questions_ExamId_SourceQuestionId",
                table: "ExamQuestions",
                newName: "IX_ExamQuestions_ExamId_SourceQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Participants_UserId",
                table: "ExamParticipants",
                newName: "IX_ExamParticipants_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Participants_ExamId_UserId",
                table: "ExamParticipants",
                newName: "IX_ExamParticipants_ExamId_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Options_SourceOptionId",
                table: "ExamOptions",
                newName: "IX_ExamOptions_SourceOptionId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Options_ExamQuestionId",
                table: "ExamOptions",
                newName: "IX_ExamOptions_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Difficulty_Profiles_CreatedById",
                table: "ExamDifficultyProfiles",
                newName: "IX_ExamDifficultyProfiles_CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Difficulty_Profile_Levels_ProfileId_DifficultyLevelId",
                table: "ExamDifficultyProfileLevels",
                newName: "IX_ExamDifficultyProfileLevels_ProfileId_DifficultyLevelId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Difficulty_Profile_Levels_DifficultyLevelId",
                table: "ExamDifficultyProfileLevels",
                newName: "IX_ExamDifficultyProfileLevels_DifficultyLevelId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Attempts_UserId",
                table: "ExamAttempts",
                newName: "IX_ExamAttempts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Exam_Attempts_ExamId_UserId_AttemptNumber",
                table: "ExamAttempts",
                newName: "IX_ExamAttempts_ExamId_UserId_AttemptNumber");

            migrationBuilder.RenameIndex(
                name: "IX_Difficulty_Levels_Code",
                table: "DifficultyLevels",
                newName: "IX_DifficultyLevels_Code");

            migrationBuilder.RenameIndex(
                name: "IX_Audit_Logs_UserId",
                table: "AuditLogs",
                newName: "IX_AuditLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Audit_Logs_OccurredAtUtc",
                table: "AuditLogs",
                newName: "IX_AuditLogs_OccurredAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Attempt_Answers_ExamQuestionId",
                table: "AttemptAnswers",
                newName: "IX_AttemptAnswers_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Attempt_Answers_AttemptId_ExamQuestionId",
                table: "AttemptAnswers",
                newName: "IX_AttemptAnswers_AttemptId_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Attempt_Answer_Options_ExamOptionId_ExamQuestionId",
                table: "AttemptAnswerOptions",
                newName: "IX_AttemptAnswerOptions_ExamOptionId_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Attempt_Answer_Options_AttemptAnswerId_ExamQuestionId",
                table: "AttemptAnswerOptions",
                newName: "IX_AttemptAnswerOptions_AttemptAnswerId_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_Ai_Question_Requests_TopicId",
                table: "AiQuestionRequests",
                newName: "IX_AiQuestionRequests_TopicId");

            migrationBuilder.RenameIndex(
                name: "IX_Ai_Question_Requests_RequestedById",
                table: "AiQuestionRequests",
                newName: "IX_AiQuestionRequests_RequestedById");

            migrationBuilder.RenameIndex(
                name: "IX_Ai_Analysis_Reports_RequestedById",
                table: "AiAnalysisReports",
                newName: "IX_AiAnalysisReports_RequestedById");

            migrationBuilder.RenameIndex(
                name: "IX_Ai_Analysis_Reports_ExamId",
                table: "AiAnalysisReports",
                newName: "IX_AiAnalysisReports_ExamId");

            migrationBuilder.RenameIndex(
                name: "IX_Ai_Analysis_Reports_AttemptId",
                table: "AiAnalysisReports",
                newName: "IX_AiAnalysisReports_AttemptId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuestionOptions",
                table: "QuestionOptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamQuestions",
                table: "ExamQuestions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamParticipants",
                table: "ExamParticipants",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ExamOptions_Id_ExamQuestionId",
                table: "ExamOptions",
                columns: new[] { "Id", "ExamQuestionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamOptions",
                table: "ExamOptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamDifficultyProfiles",
                table: "ExamDifficultyProfiles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamDifficultyProfileLevels",
                table: "ExamDifficultyProfileLevels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamAttempts",
                table: "ExamAttempts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DifficultyLevels",
                table: "DifficultyLevels",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AttemptAnswers_Id_ExamQuestionId",
                table: "AttemptAnswers",
                columns: new[] { "Id", "ExamQuestionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttemptAnswers",
                table: "AttemptAnswers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AttemptAnswerOptions",
                table: "AttemptAnswerOptions",
                columns: new[] { "AttemptAnswerId", "ExamOptionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AiQuestionRequests",
                table: "AiQuestionRequests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AiAnalysisReports",
                table: "AiAnalysisReports",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AiAnalysisReports_ExamAttempts_AttemptId",
                table: "AiAnalysisReports",
                column: "AttemptId",
                principalTable: "ExamAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AiAnalysisReports_Exams_ExamId",
                table: "AiAnalysisReports",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AiAnalysisReports_Users_RequestedById",
                table: "AiAnalysisReports",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AiQuestionRequests_Topics_TopicId",
                table: "AiQuestionRequests",
                column: "TopicId",
                principalTable: "Topics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AiQuestionRequests_Users_RequestedById",
                table: "AiQuestionRequests",
                column: "RequestedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptAnswerOptions_AttemptAnswers_AttemptAnswerId_ExamQues~",
                table: "AttemptAnswerOptions",
                columns: new[] { "AttemptAnswerId", "ExamQuestionId" },
                principalTable: "AttemptAnswers",
                principalColumns: new[] { "Id", "ExamQuestionId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptAnswerOptions_ExamOptions_ExamOptionId_ExamQuestionId",
                table: "AttemptAnswerOptions",
                columns: new[] { "ExamOptionId", "ExamQuestionId" },
                principalTable: "ExamOptions",
                principalColumns: new[] { "Id", "ExamQuestionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptAnswers_ExamAttempts_AttemptId",
                table: "AttemptAnswers",
                column: "AttemptId",
                principalTable: "ExamAttempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttemptAnswers_ExamQuestions_ExamQuestionId",
                table: "AttemptAnswers",
                column: "ExamQuestionId",
                principalTable: "ExamQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAttempts_Exams_ExamId",
                table: "ExamAttempts",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAttempts_Users_UserId",
                table: "ExamAttempts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamDifficultyProfileLevels_DifficultyLevels_DifficultyLevel~",
                table: "ExamDifficultyProfileLevels",
                column: "DifficultyLevelId",
                principalTable: "DifficultyLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamDifficultyProfileLevels_ExamDifficultyProfiles_ProfileId",
                table: "ExamDifficultyProfileLevels",
                column: "ProfileId",
                principalTable: "ExamDifficultyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamDifficultyProfiles_Users_CreatedById",
                table: "ExamDifficultyProfiles",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamOptions_ExamQuestions_ExamQuestionId",
                table: "ExamOptions",
                column: "ExamQuestionId",
                principalTable: "ExamQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamOptions_QuestionOptions_SourceOptionId",
                table: "ExamOptions",
                column: "SourceOptionId",
                principalTable: "QuestionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamParticipants_Exams_ExamId",
                table: "ExamParticipants",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamParticipants_Users_UserId",
                table: "ExamParticipants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamQuestions_Exams_ExamId",
                table: "ExamQuestions",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamQuestions_Questions_SourceQuestionId",
                table: "ExamQuestions",
                column: "SourceQuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_ExamDifficultyProfiles_DifficultyProfileId",
                table: "Exams",
                column: "DifficultyProfileId",
                principalTable: "ExamDifficultyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionOptions_Questions_QuestionId",
                table: "QuestionOptions",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_DifficultyLevels_DifficultyLevelId",
                table: "Questions",
                column: "DifficultyLevelId",
                principalTable: "DifficultyLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
