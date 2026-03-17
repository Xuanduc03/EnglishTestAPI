using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fix_all_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageAiScore",
                table: "PracticePartResults",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedGradingCount",
                table: "PracticePartResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingGradingCount",
                table: "PracticePartResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttemptType",
                table: "PracticeAttempts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "PracticeAttempts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalTimeSeconds",
                table: "PracticeAttempts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AiScore",
                table: "PracticeAnswers",
                type: "double",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageAiScore",
                table: "PracticePartResults");

            migrationBuilder.DropColumn(
                name: "FailedGradingCount",
                table: "PracticePartResults");

            migrationBuilder.DropColumn(
                name: "PendingGradingCount",
                table: "PracticePartResults");

            migrationBuilder.DropColumn(
                name: "AttemptType",
                table: "PracticeAttempts");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "PracticeAttempts");

            migrationBuilder.DropColumn(
                name: "TotalTimeSeconds",
                table: "PracticeAttempts");

            migrationBuilder.DropColumn(
                name: "AiScore",
                table: "PracticeAnswers");
        }
    }
}
