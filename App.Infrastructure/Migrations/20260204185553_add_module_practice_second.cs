using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class add_module_practice_second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "ExamStructures",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        PartName = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
            //        QuestionCount = table.Column<int>(type: "int", nullable: false),
            //        SourceCategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        SelectionMode = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        MinDifficulty = table.Column<int>(type: "int", nullable: true),
            //        MaxDifficulty = table.Column<int>(type: "int", nullable: true),
            //        ScorePerQuestion = table.Column<double>(type: "double", nullable: false),
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
            //        table.PrimaryKey("PK_ExamStructures", x => x.Id);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "categories",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        CodeType = table.Column<string>(type: "varchar(255)", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Code = table.Column<string>(type: "varchar(255)", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Name = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Description = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        ParentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
            //        table.PrimaryKey("PK_categories", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_categories_categories_ParentId",
            //            column: x => x.ParentId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "exam_results",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        StudentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        TotalScore = table.Column<double>(type: "double", nullable: false),
            //        ScoreDetailJson = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        CorrectCount = table.Column<int>(type: "int", nullable: false),
            //        TotalQuestion = table.Column<int>(type: "int", nullable: false),
            //        Status = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
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
            //        table.PrimaryKey("PK_exam_results", x => x.Id);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "exams",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Title = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Code = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Description = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Duration = table.Column<int>(type: "int", nullable: false),
            //        TotalScore = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
            //        Type = table.Column<int>(type: "int", nullable: false),
            //        Category = table.Column<int>(type: "int", nullable: false),
            //        Scope = table.Column<int>(type: "int", nullable: false),
            //        Level = table.Column<int>(type: "int", nullable: false),
            //        Tags = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        MetaData = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Status = table.Column<int>(type: "int", nullable: false),
            //        ShuffleQuestions = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        ShuffleAnswers = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        Version = table.Column<int>(type: "int", nullable: false),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
            //        table.PrimaryKey("PK_exams", x => x.Id);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "permissions",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Name = table.Column<string>(type: "varchar(255)", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Description = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Module = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
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
            //        table.PrimaryKey("PK_permissions", x => x.Id);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "roles",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Name = table.Column<string>(type: "varchar(255)", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Description = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
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
            //        table.PrimaryKey("PK_roles", x => x.Id);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "users",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Email = table.Column<string>(type: "varchar(255)", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Password = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Fullname = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Phone = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        AvatarUrl = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        AvatarPublicId = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        EmailVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
            //        LockoutEnd = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        ResetToken = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        ResetTokenExpiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        LastLogin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
            //        table.PrimaryKey("PK_users", x => x.Id);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "question_groups",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        CategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        DifficultyId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        Content = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Explanation = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Transcript = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        MediaJson = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
            //        table.PrimaryKey("PK_question_groups", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_question_groups_categories_CategoryId",
            //            column: x => x.CategoryId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //        table.ForeignKey(
            //            name: "FK_question_groups_categories_DifficultyId",
            //            column: x => x.DifficultyId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "student_answers",
            //    columns: table => new
            //    {
            //        ExamResultId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        SelectedAnswerId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        TextAnswer = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        RecordingUrl = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        WordCount = table.Column<int>(type: "int", nullable: true),
            //        IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        AiGraded = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        Feedback = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_student_answers", x => new { x.ExamResultId, x.QuestionId });
            //        table.ForeignKey(
            //            name: "FK_student_answers_exam_results_ExamResultId",
            //            column: x => x.ExamResultId,
            //            principalTable: "exam_results",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "exam_sections",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Instructions = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        CategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
            //        TimeLimit = table.Column<int>(type: "int", nullable: true),
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
            //        table.PrimaryKey("PK_exam_sections", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_exam_sections_categories_CategoryId",
            //            column: x => x.CategoryId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_exam_sections_exams_ExamId",
            //            column: x => x.ExamId,
            //            principalTable: "exams",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "score_tables",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        SkillType = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        ConversionJson = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
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
            //        table.PrimaryKey("PK_score_tables", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_score_tables_exams_ExamId",
            //            column: x => x.ExamId,
            //            principalTable: "exams",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "role_permissions",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        RoleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        PermissionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        AssignedBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_role_permissions", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_role_permissions_permissions_PermissionId",
            //            column: x => x.PermissionId,
            //            principalTable: "permissions",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_role_permissions_roles_RoleId",
            //            column: x => x.RoleId,
            //            principalTable: "roles",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "PracticeAttempts",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        CategoryId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        TimeLimitSeconds = table.Column<int>(type: "int", nullable: true),
            //        Status = table.Column<int>(type: "int", nullable: false),
            //        TotalQuestions = table.Column<int>(type: "int", nullable: false),
            //        CorrectAnswers = table.Column<int>(type: "int", nullable: false),
            //        IncorrectAnswers = table.Column<int>(type: "int", nullable: false),
            //        UnansweredQuestions = table.Column<int>(type: "int", nullable: false),
            //        Score = table.Column<double>(type: "double", precision: 10, scale: 2, nullable: false),
            //        AccuracyPercentage = table.Column<double>(type: "double", precision: 5, scale: 2, nullable: false),
            //        IsRandomOrder = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
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
            //        table.PrimaryKey("PK_PracticeAttempts", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_PracticeAttempts_categories_CategoryId",
            //            column: x => x.CategoryId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.SetNull);
            //        table.ForeignKey(
            //            name: "FK_PracticeAttempts_users_UserId",
            //            column: x => x.UserId,
            //            principalTable: "users",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "RefreshTokens",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Token = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        ExpiredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_RefreshTokens", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_RefreshTokens_users_UserId",
            //            column: x => x.UserId,
            //            principalTable: "users",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "classes",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Description = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        ScheduleInfo = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        MaxStudents = table.Column<int>(type: "int", nullable: false),
            //        TuitionFee = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
            //        Room = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Location = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        CategoryId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        Status = table.Column<int>(type: "int", nullable: false),
            //        UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_classes", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_classes_categories_CategoryId",
            //            column: x => x.CategoryId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.SetNull);
            //        table.ForeignKey(
            //            name: "FK_classes_users_CreatedBy",
            //            column: x => x.CreatedBy,
            //            principalTable: "users",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "students",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Fullname = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        CCCD = table.Column<string>(type: "varchar(255)", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Gender = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        SBD = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        School = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Streak = table.Column<int>(type: "int", nullable: false),
            //        LastStreakDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        Points = table.Column<int>(type: "int", nullable: false),
            //        MemberLevel = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
            //        table.PrimaryKey("PK_students", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_students_users_UserId",
            //            column: x => x.UserId,
            //            principalTable: "users",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "teachers",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Fullname = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        TeacherCode = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Department = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Specialty = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Title = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Degree = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Experience = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Bio = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        YearsOfExperience = table.Column<int>(type: "int", nullable: false),
            //        Status = table.Column<int>(type: "int", nullable: false),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        UserId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_teachers", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_teachers_users_UserId",
            //            column: x => x.UserId,
            //            principalTable: "users",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_teachers_users_UserId1",
            //            column: x => x.UserId1,
            //            principalTable: "users",
            //            principalColumn: "Id");
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "user_roles",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        RoleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        AssignedBy = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_user_roles", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_user_roles_roles_RoleId",
            //            column: x => x.RoleId,
            //            principalTable: "roles",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_user_roles_users_UserId",
            //            column: x => x.UserId,
            //            principalTable: "users",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "QuestionGroupMedia",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionGroupId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Url = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        PublicId = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        FileHash = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        MediaType = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
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
            //        table.PrimaryKey("PK_QuestionGroupMedia", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_QuestionGroupMedia_question_groups_QuestionGroupId",
            //            column: x => x.QuestionGroupId,
            //            principalTable: "question_groups",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "Questions",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        CategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        GroupId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        QuestionType = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        PromptTypes = table.Column<int>(type: "int", nullable: true),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        IsPublic = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        DifficultyId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        Content = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Explanation = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        TimeLimitSeconds = table.Column<int>(type: "int", nullable: true),
            //        ShuffleAnswers = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        DefaultScore = table.Column<double>(type: "double", nullable: false),
            //        MinWords = table.Column<int>(type: "int", nullable: true),
            //        MaxWords = table.Column<int>(type: "int", nullable: true),
            //        RubricJson = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
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
            //        table.PrimaryKey("PK_Questions", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_Questions_categories_CategoryId",
            //            column: x => x.CategoryId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_Questions_categories_DifficultyId",
            //            column: x => x.DifficultyId,
            //            principalTable: "categories",
            //            principalColumn: "Id");
            //        table.ForeignKey(
            //            name: "FK_Questions_question_groups_GroupId",
            //            column: x => x.GroupId,
            //            principalTable: "question_groups",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "PracticePartResults",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        PracticeAttemptId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        CategoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        PartNumber = table.Column<int>(type: "int", nullable: false),
            //        PartName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        TotalQuestions = table.Column<int>(type: "int", nullable: false),
            //        CorrectAnswers = table.Column<int>(type: "int", nullable: false),
            //        IncorrectAnswers = table.Column<int>(type: "int", nullable: false),
            //        UnansweredQuestions = table.Column<int>(type: "int", nullable: false),
            //        Percentage = table.Column<double>(type: "double", precision: 5, scale: 2, nullable: false),
            //        TotalTimeSeconds = table.Column<int>(type: "int", nullable: false),
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
            //        table.PrimaryKey("PK_PracticePartResults", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_PracticePartResults_PracticeAttempts_PracticeAttemptId",
            //            column: x => x.PracticeAttemptId,
            //            principalTable: "PracticeAttempts",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_PracticePartResults_categories_CategoryId",
            //            column: x => x.CategoryId,
            //            principalTable: "categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "class_students",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ClassId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        StudentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        EnrolledAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
            //        Status = table.Column<int>(type: "int", nullable: false),
            //        CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        StudentId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_class_students", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_class_students_classes_ClassId",
            //            column: x => x.ClassId,
            //            principalTable: "classes",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_class_students_students_StudentId",
            //            column: x => x.StudentId,
            //            principalTable: "students",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_class_students_students_StudentId1",
            //            column: x => x.StudentId1,
            //            principalTable: "students",
            //            principalColumn: "Id");
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "class_teachers",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ClassId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        TeacherId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Role = table.Column<int>(type: "int", nullable: false),
            //        IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        AssignedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
            //        TeacherId1 = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_class_teachers", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_class_teachers_classes_ClassId",
            //            column: x => x.ClassId,
            //            principalTable: "classes",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_class_teachers_teachers_TeacherId",
            //            column: x => x.TeacherId,
            //            principalTable: "teachers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_class_teachers_teachers_TeacherId1",
            //            column: x => x.TeacherId1,
            //            principalTable: "teachers",
            //            principalColumn: "Id");
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "QuestionMedias",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Url = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        PublicId = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        MediaType = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        FileHash = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
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
            //        table.PrimaryKey("PK_QuestionMedias", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_QuestionMedias_Questions_QuestionId",
            //            column: x => x.QuestionId,
            //            principalTable: "Questions",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "answers",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        Content = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
            //        Feedback = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Explanation = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        ScoreWeight = table.Column<double>(type: "double", nullable: true),
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
            //        table.PrimaryKey("PK_answers", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_answers_Questions_QuestionId",
            //            column: x => x.QuestionId,
            //            principalTable: "Questions",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "exam_questions",
            //    columns: table => new
            //    {
            //        ExamSectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionNo = table.Column<int>(type: "int", nullable: false),
            //        Point = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
            //        IsMandatory = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        IsShuffleable = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
            //        table.PrimaryKey("PK_exam_questions", x => new { x.ExamSectionId, x.QuestionId });
            //        table.ForeignKey(
            //            name: "FK_exam_questions_Questions_QuestionId",
            //            column: x => x.QuestionId,
            //            principalTable: "Questions",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //        table.ForeignKey(
            //            name: "FK_exam_questions_exam_sections_ExamSectionId",
            //            column: x => x.ExamSectionId,
            //            principalTable: "exam_sections",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_exam_questions_exams_ExamId",
            //            column: x => x.ExamId,
            //            principalTable: "exams",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "question_tags",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        QuestionGroupId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        Tag = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        TagType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
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
            //        table.PrimaryKey("PK_question_tags", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_question_tags_Questions_QuestionId",
            //            column: x => x.QuestionId,
            //            principalTable: "Questions",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_question_tags_question_groups_QuestionGroupId",
            //            column: x => x.QuestionGroupId,
            //            principalTable: "question_groups",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "PracticeAnswers",
            //    columns: table => new
            //    {
            //        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        PracticeAttemptId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            //        SelectedAnswerId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
            //        IsCorrect = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        IsMarkedForReview = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
            //        AnsweredAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        TimeSpentSeconds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
            //        OrderIndex = table.Column<int>(type: "int", nullable: false),
            //        ChangeCount = table.Column<int>(type: "int", nullable: true),
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
            //        table.PrimaryKey("PK_PracticeAnswers", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_PracticeAnswers_PracticeAttempts_PracticeAttemptId",
            //            column: x => x.PracticeAttemptId,
            //            principalTable: "PracticeAttempts",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //        table.ForeignKey(
            //            name: "FK_PracticeAnswers_Questions_QuestionId",
            //            column: x => x.QuestionId,
            //            principalTable: "Questions",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //        table.ForeignKey(
            //            name: "FK_PracticeAnswers_answers_SelectedAnswerId",
            //            column: x => x.SelectedAnswerId,
            //            principalTable: "answers",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAnswers_AttemptId",
            //    table: "PracticeAnswers",
            //    column: "PracticeAttemptId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAnswers_Attempt_Order",
            //    table: "PracticeAnswers",
            //    columns: new[] { "PracticeAttemptId", "OrderIndex" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAnswers_QuestionId",
            //    table: "PracticeAnswers",
            //    column: "QuestionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAnswers_Question_IsCorrect",
            //    table: "PracticeAnswers",
            //    columns: new[] { "QuestionId", "IsCorrect" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAnswers_SelectedAnswerId",
            //    table: "PracticeAnswers",
            //    column: "SelectedAnswerId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAttempts_CategoryId",
            //    table: "PracticeAttempts",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAttempts_Category_Score",
            //    table: "PracticeAttempts",
            //    columns: new[] { "CategoryId", "Score" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAttempts_Status",
            //    table: "PracticeAttempts",
            //    column: "Status");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAttempts_UserId",
            //    table: "PracticeAttempts",
            //    column: "UserId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticeAttempts_User_StartedAt",
            //    table: "PracticeAttempts",
            //    columns: new[] { "UserId", "StartedAt" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticePartResults_AttemptId",
            //    table: "PracticePartResults",
            //    column: "PracticeAttemptId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticePartResults_Attempt_PartNumber",
            //    table: "PracticePartResults",
            //    columns: new[] { "PracticeAttemptId", "PartNumber" });

            //migrationBuilder.CreateIndex(
            //    name: "IX_PracticePartResults_CategoryId",
            //    table: "PracticePartResults",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_QuestionGroupMedia_QuestionGroupId",
            //    table: "QuestionGroupMedia",
            //    column: "QuestionGroupId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_QuestionMedias_QuestionId",
            //    table: "QuestionMedias",
            //    column: "QuestionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Questions_CategoryId",
            //    table: "Questions",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Questions_DifficultyId",
            //    table: "Questions",
            //    column: "DifficultyId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Questions_GroupId",
            //    table: "Questions",
            //    column: "GroupId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_RefreshTokens_UserId",
            //    table: "RefreshTokens",
            //    column: "UserId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_answers_QuestionId",
            //    table: "answers",
            //    column: "QuestionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_categories_Code",
            //    table: "categories",
            //    column: "Code");

            //migrationBuilder.CreateIndex(
            //    name: "IX_categories_CodeType",
            //    table: "categories",
            //    column: "CodeType");

            //migrationBuilder.CreateIndex(
            //    name: "IX_categories_ParentId",
            //    table: "categories",
            //    column: "ParentId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_class_students_ClassId",
            //    table: "class_students",
            //    column: "ClassId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_class_students_StudentId",
            //    table: "class_students",
            //    column: "StudentId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_class_students_StudentId1",
            //    table: "class_students",
            //    column: "StudentId1");

            //migrationBuilder.CreateIndex(
            //    name: "IX_class_teachers_ClassId",
            //    table: "class_teachers",
            //    column: "ClassId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_class_teachers_TeacherId",
            //    table: "class_teachers",
            //    column: "TeacherId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_class_teachers_TeacherId1",
            //    table: "class_teachers",
            //    column: "TeacherId1");

            //migrationBuilder.CreateIndex(
            //    name: "IX_classes_CategoryId",
            //    table: "classes",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_classes_Code",
            //    table: "classes",
            //    column: "Code",
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_classes_CreatedBy",
            //    table: "classes",
            //    column: "CreatedBy");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_questions_ExamId",
            //    table: "exam_questions",
            //    column: "ExamId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_questions_QuestionId",
            //    table: "exam_questions",
            //    column: "QuestionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_sections_CategoryId",
            //    table: "exam_sections",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_exam_sections_ExamId",
            //    table: "exam_sections",
            //    column: "ExamId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_permissions_Name",
            //    table: "permissions",
            //    column: "Name",
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_question_groups_CategoryId",
            //    table: "question_groups",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_question_groups_DifficultyId",
            //    table: "question_groups",
            //    column: "DifficultyId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_question_tags_QuestionGroupId",
            //    table: "question_tags",
            //    column: "QuestionGroupId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_question_tags_QuestionId",
            //    table: "question_tags",
            //    column: "QuestionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_question_tags_Tag",
            //    table: "question_tags",
            //    column: "Tag");

            //migrationBuilder.CreateIndex(
            //    name: "IX_question_tags_TagType",
            //    table: "question_tags",
            //    column: "TagType");

            //migrationBuilder.CreateIndex(
            //    name: "IX_role_permissions_PermissionId",
            //    table: "role_permissions",
            //    column: "PermissionId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_role_permissions_RoleId_PermissionId",
            //    table: "role_permissions",
            //    columns: new[] { "RoleId", "PermissionId" },
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_roles_Name",
            //    table: "roles",
            //    column: "Name",
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_score_tables_ExamId",
            //    table: "score_tables",
            //    column: "ExamId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_students_CCCD",
            //    table: "students",
            //    column: "CCCD");

            //migrationBuilder.CreateIndex(
            //    name: "IX_students_UserId",
            //    table: "students",
            //    column: "UserId",
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_teachers_UserId",
            //    table: "teachers",
            //    column: "UserId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_teachers_UserId1",
            //    table: "teachers",
            //    column: "UserId1",
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_user_roles_RoleId",
            //    table: "user_roles",
            //    column: "RoleId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_user_roles_UserId_RoleId",
            //    table: "user_roles",
            //    columns: new[] { "UserId", "RoleId" },
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_users_Email",
            //    table: "users",
            //    column: "Email",
            //    unique: true);
        }

        ///// <inheritdoc />
        //protected override void Down(MigrationBuilder migrationBuilder)
        //{
        //    migrationBuilder.DropTable(
        //        name: "ExamStructures");

        //    migrationBuilder.DropTable(
        //        name: "PracticeAnswers");

        //    migrationBuilder.DropTable(
        //        name: "PracticePartResults");

        //    migrationBuilder.DropTable(
        //        name: "QuestionGroupMedia");

        //    migrationBuilder.DropTable(
        //        name: "QuestionMedias");

        //    migrationBuilder.DropTable(
        //        name: "RefreshTokens");

        //    migrationBuilder.DropTable(
        //        name: "class_students");

        //    migrationBuilder.DropTable(
        //        name: "class_teachers");

        //    migrationBuilder.DropTable(
        //        name: "exam_questions");

        //    migrationBuilder.DropTable(
        //        name: "question_tags");

        //    migrationBuilder.DropTable(
        //        name: "role_permissions");

        //    migrationBuilder.DropTable(
        //        name: "score_tables");

        //    migrationBuilder.DropTable(
        //        name: "student_answers");

        //    migrationBuilder.DropTable(
        //        name: "user_roles");

        //    migrationBuilder.DropTable(
        //        name: "answers");

        //    migrationBuilder.DropTable(
        //        name: "PracticeAttempts");

        //    migrationBuilder.DropTable(
        //        name: "students");

        //    migrationBuilder.DropTable(
        //        name: "classes");

        //    migrationBuilder.DropTable(
        //        name: "teachers");

        //    migrationBuilder.DropTable(
        //        name: "exam_sections");

        //    migrationBuilder.DropTable(
        //        name: "permissions");

        //    migrationBuilder.DropTable(
        //        name: "exam_results");

        //    migrationBuilder.DropTable(
        //        name: "roles");

        //    migrationBuilder.DropTable(
        //        name: "Questions");

        //    migrationBuilder.DropTable(
        //        name: "users");

        //    migrationBuilder.DropTable(
        //        name: "exams");

        //    migrationBuilder.DropTable(
        //        name: "question_groups");

        //    migrationBuilder.DropTable(
        //        name: "categories");
        //}
    }
}
