using App.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Persistence.Configurations
{
    public class PracticeAttemptConfiguration : IEntityTypeConfiguration<PracticeAttempt>
    {
        public void Configure(EntityTypeBuilder<PracticeAttempt> builder)
        {
            builder.HasKey(x => x.Id);

            // ============================================
            // PROPERTIES
            // ============================================

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.StartedAt)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Score)
                .HasPrecision(10, 2);

            builder.Property(x => x.AccuracyPercentage)
                .HasPrecision(5, 2);

            builder.Property(x => x.Notes)
                .HasMaxLength(1000);

            // ============================================
            // INDEXES
            // ============================================

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_PracticeAttempts_UserId");

            builder.HasIndex(x => x.CategoryId)
                .HasDatabaseName("IX_PracticeAttempts_CategoryId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_PracticeAttempts_Status");

            builder.HasIndex(x => new { x.UserId, x.StartedAt })
                .HasDatabaseName("IX_PracticeAttempts_User_StartedAt");

            // For leaderboard queries
            builder.HasIndex(x => new { x.CategoryId, x.Score })
                .HasDatabaseName("IX_PracticeAttempts_Category_Score");

            // ============================================
            // RELATIONSHIPS
            // ============================================

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Answers)
                .WithOne(x => x.PracticeAttempt)
                .HasForeignKey(x => x.PracticeAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.PartResults)
                .WithOne(x => x.PracticeAttempt)
                .HasForeignKey(x => x.PracticeAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PracticeAnswerConfiguration : IEntityTypeConfiguration<PracticeAnswer>
    {
        public void Configure(EntityTypeBuilder<PracticeAnswer> builder)
        {
            builder.HasKey(x => x.Id);

            // ============================================
            // PROPERTIES
            // ============================================

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

            // ============================================
            // INDEXES
            // ============================================

            builder.HasIndex(x => x.PracticeAttemptId)
                .HasDatabaseName("IX_PracticeAnswers_AttemptId");

            builder.HasIndex(x => x.QuestionId)
                .HasDatabaseName("IX_PracticeAnswers_QuestionId");

            builder.HasIndex(x => new { x.PracticeAttemptId, x.OrderIndex })
                .HasDatabaseName("IX_PracticeAnswers_Attempt_Order");

            // For analytics: Find most difficult questions
            builder.HasIndex(x => new { x.QuestionId, x.IsCorrect })
                .HasDatabaseName("IX_PracticeAnswers_Question_IsCorrect");

            // ============================================
            // RELATIONSHIPS
            // ============================================

            builder.HasOne(x => x.PracticeAttempt)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.PracticeAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);  // Don't delete questions

            builder.HasOne(x => x.SelectedAnswer)
                .WithMany()
                .HasForeignKey(x => x.SelectedAnswerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PracticePartResultConfiguration : IEntityTypeConfiguration<PracticePartResult>
    {
        public void Configure(EntityTypeBuilder<PracticePartResult> builder)
        {
            builder.HasKey(x => x.Id);

            // ============================================
            // PROPERTIES
            // ============================================

            builder.Property(x => x.PartNumber)
                .IsRequired();

            builder.Property(x => x.PartName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Percentage)
                .HasPrecision(5, 2);

            // ============================================
            // INDEXES
            // ============================================

            builder.HasIndex(x => x.PracticeAttemptId)
                .HasDatabaseName("IX_PracticePartResults_AttemptId");

            builder.HasIndex(x => new { x.PracticeAttemptId, x.PartNumber })
                .HasDatabaseName("IX_PracticePartResults_Attempt_PartNumber");

            // ============================================
            // RELATIONSHIPS
            // ============================================

            builder.HasOne(x => x.PracticeAttempt)
                .WithMany(x => x.PartResults)
                .HasForeignKey(x => x.PracticeAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
