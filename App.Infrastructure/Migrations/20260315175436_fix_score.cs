using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fix_score : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          
                    
          

         

            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "VocabularyWords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "VocabularyWords",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "Example",
                table: "VocabularyWords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExampleMeaning",
                table: "VocabularyWords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "VocabularyWords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "VocabularyWords",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "EaseFactor",
                table: "UserVocabularyProgresses",
                type: "double",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Interval",
                table: "UserVocabularyProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RepetitionCount",
                table: "UserVocabularyProgresses",
                type: "int",
                nullable: false,
                defaultValue: 0);

         
            migrationBuilder.CreateTable(
                name: "score_table_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ScoreTableId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_score_table_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_score_table_entries_score_tables_ScoreTableId",
                        column: x => x.ScoreTableId,
                        principalTable: "score_tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyWords_CategoryId",
                table: "VocabularyWords",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_score_table_entries_ScoreTableId_CorrectAnswers",
                table: "score_table_entries",
                columns: new[] { "ScoreTableId", "CorrectAnswers" },
                unique: true);

           

            migrationBuilder.AddForeignKey(
                name: "FK_VocabularyWords_categories_CategoryId",
                table: "VocabularyWords",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id");

         
            migrationBuilder.AddForeignKey(
                name: "FK_score_tables_categories_SkillCategoryId",
                table: "score_tables",
                column: "SkillCategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeAnswers_questions_QuestionId",
                table: "PracticeAnswers");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestionMedias_questions_QuestionId",
                table: "QuestionMedias");

            migrationBuilder.DropForeignKey(
                name: "FK_VocabularyWords_categories_CategoryId",
                table: "VocabularyWords");

            migrationBuilder.DropForeignKey(
                name: "FK_answers_questions_QuestionId",
                table: "answers");

            migrationBuilder.DropForeignKey(
                name: "FK_exam_questions_questions_QuestionId",
                table: "exam_questions");

            migrationBuilder.DropForeignKey(
                name: "FK_question_tags_questions_QuestionId",
                table: "question_tags");

            migrationBuilder.DropForeignKey(
                name: "FK_questions_categories_CategoryId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_questions_categories_DifficultyId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_questions_question_groups_GroupId",
                table: "questions");

            migrationBuilder.DropForeignKey(
                name: "FK_score_tables_categories_SkillCategoryId",
                table: "score_tables");

            migrationBuilder.DropTable(
                name: "score_table_entries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_questions",
                table: "questions");

            migrationBuilder.DropIndex(
                name: "IX_VocabularyWords_CategoryId",
                table: "VocabularyWords");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "score_tables");

            migrationBuilder.DropColumn(
                name: "MaxScore",
                table: "score_tables");

            migrationBuilder.DropColumn(
                name: "MinScore",
                table: "score_tables");

            migrationBuilder.DropColumn(
                name: "ActualWordCount",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "AiPromptTemplate",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "IsAiGraded",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "SampleAnswer",
                table: "questions");

            migrationBuilder.DropColumn(
                name: "AiFeedback",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "AiScoreDetailJson",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "AudioPublicId",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "GradingStatus",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "IsAiGraded",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "RecordingDurationSeconds",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "exam_answers");

            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "VocabularyWords");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "VocabularyWords");

            migrationBuilder.DropColumn(
                name: "Example",
                table: "VocabularyWords");

            migrationBuilder.DropColumn(
                name: "ExampleMeaning",
                table: "VocabularyWords");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "VocabularyWords");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "VocabularyWords");

            migrationBuilder.DropColumn(
                name: "EaseFactor",
                table: "UserVocabularyProgresses");

            migrationBuilder.DropColumn(
                name: "Interval",
                table: "UserVocabularyProgresses");

            migrationBuilder.DropColumn(
                name: "RepetitionCount",
                table: "UserVocabularyProgresses");

            migrationBuilder.DropColumn(
                name: "AiFeedback",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "AiScoreDetailJson",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "AudioPublicId",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "GradedAt",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "GradingStatus",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "IsAiGraded",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "RecordingDurationSeconds",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "TextAnswer",
                table: "PracticeAnswers");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "PracticeAnswers");

            migrationBuilder.RenameTable(
                name: "questions",
                newName: "Questions");

            migrationBuilder.RenameColumn(
                name: "SkillCategoryId",
                table: "score_tables",
                newName: "ExamId");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "score_tables",
                newName: "ConversionJson");

            migrationBuilder.RenameIndex(
                name: "IX_score_tables_SkillCategoryId",
                table: "score_tables",
                newName: "IX_score_tables_ExamId");

            migrationBuilder.RenameIndex(
                name: "IX_questions_GroupId",
                table: "Questions",
                newName: "IX_Questions_GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_questions_DifficultyId",
                table: "Questions",
                newName: "IX_Questions_DifficultyId");

            migrationBuilder.RenameIndex(
                name: "IX_questions_CategoryId",
                table: "Questions",
                newName: "IX_Questions_CategoryId");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "score_tables",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AlterColumn<int>(
                name: "PromptTypes",
                table: "Questions",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Questions",
                table: "Questions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_score_tables_CategoryId",
                table: "score_tables",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeAnswers_Questions_QuestionId",
                table: "PracticeAnswers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionMedias_Questions_QuestionId",
                table: "QuestionMedias",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_categories_CategoryId",
                table: "Questions",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_categories_DifficultyId",
                table: "Questions",
                column: "DifficultyId",
                principalTable: "categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_question_groups_GroupId",
                table: "Questions",
                column: "GroupId",
                principalTable: "question_groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_answers_Questions_QuestionId",
                table: "answers",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exam_questions_Questions_QuestionId",
                table: "exam_questions",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_question_tags_Questions_QuestionId",
                table: "question_tags",
                column: "QuestionId",
                principalTable: "Questions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_score_tables_categories_CategoryId",
                table: "score_tables",
                column: "CategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_score_tables_exams_ExamId",
                table: "score_tables",
                column: "ExamId",
                principalTable: "exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
