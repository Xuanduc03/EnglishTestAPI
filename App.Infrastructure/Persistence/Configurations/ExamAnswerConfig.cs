using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Setup config cho đáp án của bài thi
    /// </summary>
    public class ExamAnswerConfig : IEntityTypeConfiguration<ExamAnswer>
    {
        public void Configure(EntityTypeBuilder<ExamAnswer> builder)
        {
            // Table
            builder.ToTable("exam_answers");

            // Primary key
            builder.HasKey(e => e.Id);

            // Required fields
            builder.Property(e => e.ExamAttemptId)
                .IsRequired();

            builder.Property(e => e.ExamQuestionId)
                .IsRequired();

            builder.Property(e => e.QuestionId)
                .IsRequired();

            builder.Property(e => e.IsAnswered)
                .IsRequired();

            builder.Property(e => e.IsCorrect)
                .IsRequired();

            // Point
            builder.Property(e => e.Point)
                .HasColumnType("decimal(5,2)");

            // ExamAttempt relationship
            builder.HasOne(e => e.Attempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(e => e.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamQuestion relationship
            builder.HasOne(e => e.ExamQuestions)
                .WithMany(q => q.ExamAnswers)
                .HasForeignKey(e => e.ExamQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Grading status
            builder.Property(e => e.GradingStatus)
                .HasConversion<string>();

            // Text answer
            builder.Property(e => e.TextAnswer)
                .HasColumnType("longtext");

            // AI feedback
            builder.Property(e => e.AiFeedback)
                .HasColumnType("longtext");

            // AI score detail JSON
            builder.Property(e => e.AiScoreDetailJson)
                .HasColumnType("longtext");

            // Unique:
            // 1 attempt chỉ có 1 answer cho mỗi ExamQuestion
            builder.HasIndex(e => new
            {
                e.ExamAttemptId,
                e.ExamQuestionId
            })
            .IsUnique();

            // Index để query theo Attempt
            builder.HasIndex(e => e.ExamAttemptId);
        }
    }
}