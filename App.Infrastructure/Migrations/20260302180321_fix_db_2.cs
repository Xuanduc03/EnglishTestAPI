using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fix_db_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

          
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_answers_exam_attemptss_ExamAttemptId",
                table: "exam_answers");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_answers_exam_questions_ExamQuestionId",
                table: "exam_answers");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_attemptss_exams_ExamId",
                table: "exam_attemptss");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_attemptss_users_UserId",
                table: "exam_attemptss");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_results_users_UserId",
                table: "exam_results");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_section_results_exam_attemptss_ExamAttemptId",
                table: "exam_section_results");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_section_results_exam_sections_ExamSectionId",
                table: "exam_section_results");

            migrationBuilder.DropTable(
                name: "UserVocabularyProgresses");

            migrationBuilder.DropTable(
                name: "VocabularyWords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exam_section_results",
                table: "exam_section_results");

            migrationBuilder.DropIndex(
                name: "IX_exam_section_results_ExamAttemptId_ExamSectionId",
                table: "exam_section_results");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exam_attemptss",
                table: "exam_attemptss");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exam_answers",
                table: "exam_answers");

            migrationBuilder.DropIndex(
                name: "IX_exam_answers_ExamAttemptId_ExamQuestionId",
                table: "exam_answers");



            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "exam_results",
                newName: "StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_exam_results_UserId",
                table: "exam_results",
                newName: "IX_exam_results_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_exam_section_results_ExamSectionId",
                table: "exam_section_results",
                newName: "IX_exam_section_results_ExamSectionId");

            migrationBuilder.RenameIndex(
                name: "IX_exam_attemptss_UserId",
                table: "exam_attempts",
                newName: "IX_exam_attempts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_exam_attemptss_ExamId",
                table: "exam_attempts",
                newName: "IX_exam_attempts_ExamId");

            migrationBuilder.RenameIndex(
                name: "IX_exam_answers_ExamQuestionId",
                table: "exam_answers",
                newName: "IX_exam_answers_ExamQuestionId");

            migrationBuilder.RenameIndex(
                name: "IX_exam_answers_ExamAttemptId",
                table: "exam_answers",
                newName: "IX_exam_answers_ExamAttemptId");

            migrationBuilder.AlterColumn<byte[]>(
                name: "VersionNumber",
                table: "exam_attempts",
                type: "longblob",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp(6)",
                oldRowVersion: true);

            migrationBuilder.AddColumn<string>(
                name: "AntiCheatFlags",
                table: "exam_attempts",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "exam_attempts",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PageReloadCount",
                table: "exam_attempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TabSwitchCount",
                table: "exam_attempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "exam_attempts",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<double>(
                name: "Point",
                table: "exam_answers",
                type: "double",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_exam_section_results",
                table: "exam_section_results",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_exam_attempts",
                table: "exam_attempts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_exam_answers",
                table: "exam_answers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_exam_section_results_ExamAttemptId",
                table: "exam_section_results",
                column: "ExamAttemptId");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_answers_exam_attempts_ExamAttemptId",
                table: "exam_answers",
                column: "ExamAttemptId",
                principalTable: "exam_attempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_answers_exam_questions_ExamQuestionId",
                table: "exam_answers",
                column: "ExamQuestionId",
                principalTable: "exam_questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_attempts_exams_ExamId",
                table: "exam_attempts",
                column: "ExamId",
                principalTable: "exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_attempts_users_UserId",
                table: "exam_attempts",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_results_students_StudentId",
                table: "exam_results",
                column: "StudentId",
                principalTable: "students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_section_results_exam_attempts_ExamAttemptId",
                table: "exam_section_results",
                column: "ExamAttemptId",
                principalTable: "exam_attempts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_section_results_exam_sections_ExamSectionId",
                table: "exam_section_results",
                column: "ExamSectionId",
                principalTable: "exam_sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
