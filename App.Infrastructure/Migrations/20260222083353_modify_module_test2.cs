using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modify_module_test2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropTable(
            //    name: "ExamStructures");

            //migrationBuilder.DropTable(
            //    name: "class_students");

            //migrationBuilder.DropTable(
            //    name: "class_teachers");

            //migrationBuilder.DropTable(
            //    name: "student_answers");

            //migrationBuilder.DropTable(
            //    name: "classes");

            //migrationBuilder.DropTable(
            //    name: "teachers");

            //migrationBuilder.DropPrimaryKey(
            //    name: "PK_exam_questions",
            //    table: "exam_questions");

            //migrationBuilder.DropColumn(
            //    name: "School",
            //    table: "students");

            //migrationBuilder.DropColumn(
            //    name: "SkillType",
            //    table: "score_tables");

            //migrationBuilder.AddColumn<Guid>(
            //    name: "CategoryId",
            //    table: "score_tables",
            //    type: "char(36)",
            //    nullable: true,
            //    collation: "ascii_general_ci");

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "EndDate",
            //    table: "exams",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "StartDate",
            //    table: "exams",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<int>(
            //    name: "ActualTimeSeconds",
            //    table: "PracticeAttempts",
            //    type: "int",
            //    nullable: true);

            //migrationBuilder.AddPrimaryKey(
            //    name: "PK_exam_questions",
            //    table: "exam_questions",
            //    column: "Id");

            //migrationBuilder.CreateTable(
            //    name: "exam_attempt",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        SubmitedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        TimeLimitSeconds = table.Column<int>(type: "int", nullable: false),
            //        ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        Status = table.Column<int>(type: "int", nullable: false),
            //        ActualTimeSeconds = table.Column<int>(type: "int", nullable: true),
            //        ListeningCorrect = table.Column<int>(type: "int", nullable: true),
            //        ReadingCorrect = table.Column<int>(type: "int", nullable: true),
            //        ListeningScore = table.Column<int>(type: "int", nullable: true),
            //        ReadingScore = table.Column<int>(type: "int", nullable: true),
            //        TotalScore = table.Column<int>(type: "int", nullable: true),
            //        TotalQuestions = table.Column<int>(type: "int", nullable: false),
            //        CorrectAnswers = table.Column<int>(type: "int", nullable: false),
            //        IncorrectAnswers = table.Column<int>(type: "int", nullable: false),
            //        UnanswerQuestions = table.Column<int>(type: "int", nullable: false),
            //        LastAnsweredIndex = table.Column<int>(type: "int", nullable: true),
            //        ProgressSnapshot = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        IpAddress = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        UserAgent = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        AntiCheatFlags = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        TabSwitchCount = table.Column<int>(type: "int", nullable: false),
            //        PageReloadCount = table.Column<int>(type: "int", nullable: false),
            //        VersionNumber = table.Column<byte[]>(type: "longblob", nullable: false),
            //        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_exam_attempt", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_exam_attempt_exams_ExamId",
            //            column: x => x.ExamId,
            //            principalTable: "exams",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_exam_attempt_users_UserId",
            //            column: x => x.UserId,
            //            principalTable: "users",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "exam_answer",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamAttemptId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamQuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        SelectedAnswerId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        IsAnswered = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        Point = table.Column<double>(type: "double", nullable: false),
            //        TimeSpentSeconds = table.Column<int>(type: "int", nullable: true),
            //        AnsweredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        VersionNumber = table.Column<int>(type: "int", nullable: false),
            //        CorrectAnswerId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_exam_answer", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_exam_answer_exam_attempt_ExamAttemptId",
            //            column: x => x.ExamAttemptId,
            //            principalTable: "exam_attempt",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_exam_answer_exam_questions_ExamQuestionId",
            //            column: x => x.ExamQuestionId,
            //            principalTable: "exam_questions",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "exam_section_result",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamAttemptId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamSectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        TotalQuestions = table.Column<int>(type: "int", nullable: false),
            //        CorrectAnswers = table.Column<int>(type: "int", nullable: false),
            //        ConvertedScore = table.Column<int>(type: "int", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_exam_section_result", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_exam_section_result_exam_attempt_ExamAttemptId",
            //            column: x => x.ExamAttemptId,
            //            principalTable: "exam_attempt",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_exam_section_result_exam_sections_ExamSectionId",
            //            column: x => x.ExamSectionId,
            //            principalTable: "exam_sections",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateIndex(
            //    name: "IX_score_tables_CategoryId",
            //    table: "score_tables",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_questions_ExamSectionId",
            //    table: "exam_questions",
            //    column: "ExamSectionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_answer_ExamAttemptId",
            //    table: "exam_answer",
            //    column: "ExamAttemptId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_answer_ExamQuestionId",
            //    table: "exam_answer",
            //    column: "ExamQuestionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_attempt_ExamId",
            //    table: "exam_attempt",
            //    column: "ExamId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_attempt_UserId",
            //    table: "exam_attempt",
            //    column: "UserId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_section_result_ExamAttemptId",
            //    table: "exam_section_result",
            //    column: "ExamAttemptId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_section_result_ExamSectionId",
            //    table: "exam_section_result",
            //    column: "ExamSectionId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_score_tables_categories_CategoryId",
            //    table: "score_tables",
            //    column: "CategoryId",
            //    principalTable: "categories",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_score_tables_categories_CategoryId",
                table: "score_tables");

            migrationBuilder.DropTable(
                name: "exam_answer");

            migrationBuilder.DropTable(
                name: "exam_section_result");

            migrationBuilder.DropTable(
                name: "exam_attempt");

            migrationBuilder.DropIndex(
                name: "IX_score_tables_CategoryId",
                table: "score_tables");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exam_questions",
                table: "exam_questions");

            migrationBuilder.DropIndex(
                name: "IX_exam_questions_ExamSectionId",
                table: "exam_questions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "score_tables");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "exams");

            migrationBuilder.DropColumn(
                name: "ActualTimeSeconds",
                table: "PracticeAttempts");

            migrationBuilder.AddColumn<string>(
                name: "School",
                table: "students",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SkillType",
                table: "score_tables",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_exam_questions",
                table: "exam_questions",
                columns: new[] { "ExamSectionId", "QuestionId" });

            migrationBuilder.CreateTable(
                name: "ExamStructures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxDifficulty = table.Column<int>(type: "int", nullable: true),
                    MinDifficulty = table.Column<int>(type: "int", nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    PartName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    ScorePerQuestion = table.Column<double>(type: "double", nullable: false),
                    SelectionMode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceCategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamStructures", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "classes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CategoryId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Location = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxStudents = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Room = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScheduleInfo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TuitionFee = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_classes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_classes_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_classes_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "student_answers",
                columns: table => new
                {
                    ExamResultId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AiGraded = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Feedback = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RecordingUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SelectedAnswerId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TextAnswer = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    WordCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_answers", x => new { x.ExamResultId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_student_answers_exam_results_ExamResultId",
                        column: x => x.ExamResultId,
                        principalTable: "exam_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Bio = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Degree = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Department = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Experience = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fullname = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Specialty = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TeacherCode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UserId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_teachers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_teachers_users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "class_students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClassId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StudentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    EnrolledAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StudentId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_students_classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_class_students_students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_class_students_students_StudentId1",
                        column: x => x.StudentId1,
                        principalTable: "students",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "class_teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClassId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TeacherId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    TeacherId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_teachers_classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_class_teachers_teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_class_teachers_teachers_TeacherId1",
                        column: x => x.TeacherId1,
                        principalTable: "teachers",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_class_students_ClassId",
                table: "class_students",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_class_students_StudentId",
                table: "class_students",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_class_students_StudentId1",
                table: "class_students",
                column: "StudentId1");

            migrationBuilder.CreateIndex(
                name: "IX_class_teachers_ClassId",
                table: "class_teachers",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_class_teachers_TeacherId",
                table: "class_teachers",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_class_teachers_TeacherId1",
                table: "class_teachers",
                column: "TeacherId1");

            migrationBuilder.CreateIndex(
                name: "IX_classes_CategoryId",
                table: "classes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_classes_Code",
                table: "classes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_classes_CreatedBy",
                table: "classes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_teachers_UserId",
                table: "teachers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_teachers_UserId1",
                table: "teachers",
                column: "UserId1",
                unique: true);
        }
    }
}
