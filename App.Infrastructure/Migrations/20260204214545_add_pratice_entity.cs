using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_pratice_entity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_sections_categories_CategoryId",
                table: "exam_sections");

            migrationBuilder.CreateIndex(
                name: "IX_exam_results_ExamId",
                table: "exam_results",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_results_StudentId",
                table: "exam_results",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_results_exams_ExamId",
                table: "exam_results",
                column: "ExamId",
                principalTable: "exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_results_students_StudentId",
                table: "exam_results",
                column: "StudentId",
                principalTable: "students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_sections_categories_CategoryId",
                table: "exam_sections",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_results_exams_ExamId",
                table: "exam_results");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_results_students_StudentId",
                table: "exam_results");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_sections_categories_CategoryId",
                table: "exam_sections");

            migrationBuilder.DropIndex(
                name: "IX_exam_results_ExamId",
                table: "exam_results");

            migrationBuilder.DropIndex(
                name: "IX_exam_results_StudentId",
                table: "exam_results");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_sections_categories_CategoryId",
                table: "exam_sections",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
