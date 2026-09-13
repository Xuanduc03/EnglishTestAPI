using App.Domain.Domain.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class PracticeAnswerConfig : IEntityTypeConfiguration<PracticeAnswer>
    {
        public void Configure(EntityTypeBuilder<PracticeAnswer> builder)
        {
            builder.ToTable("practice_answers");

            builder.HasKey(x => x.Id);

            // ==============================
            // Properties
            // ==============================

            builder.Property(x => x.IsCorrect)
                .IsRequired();

            builder.Property(x => x.IsMarkedForReview)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.TimeSpentSeconds)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.OrderIndex)
                .IsRequired();

            builder.Property(x => x.GradingStatus)
                .HasConversion<string>();

            builder.Property(x => x.TextAnswer)
                .HasColumnType("longtext");

            builder.Property(x => x.AiFeedback)
                .HasColumnType("longtext");

            builder.Property(x => x.AiScoreDetailJson)
                .HasColumnType("longtext");

            // ==============================
            // Indexes
            // ==============================

            builder.HasIndex(x => x.PracticeAttemptId)
                .HasDatabaseName("IX_PracticeAnswers_AttemptId");

            builder.HasIndex(x => x.QuestionId)
                .HasDatabaseName("IX_PracticeAnswers_QuestionId");

            builder.HasIndex(x => new { x.PracticeAttemptId, x.OrderIndex })
                .HasDatabaseName("IX_PracticeAnswers_Attempt_Order");

            builder.HasIndex(x => new { x.QuestionId, x.IsCorrect })
                .HasDatabaseName("IX_PracticeAnswers_Question_IsCorrect");

            // ==============================
            // Relationships
            // ==============================

            builder.HasOne(x => x.PracticeAttempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.PracticeAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SelectedAnswer)
                .WithMany()
                .HasForeignKey(x => x.SelectedAnswerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}