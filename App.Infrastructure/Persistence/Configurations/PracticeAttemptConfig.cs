using App.Domain.Domain.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class PracticeAttemptConfig : IEntityTypeConfiguration<PracticeAttempt>
    {
        public void Configure(EntityTypeBuilder<PracticeAttempt> builder)
        {
            builder.ToTable("practice_attempts");

            builder.HasKey(x => x.Id);

            // ==============================
            // Properties
            // ==============================

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

            // ==============================
            // Indexes
            // ==============================

            builder.HasIndex(x => x.UserId)
                .HasDatabaseName("IX_PracticeAttempts_UserId");

            builder.HasIndex(x => x.CategoryId)
                .HasDatabaseName("IX_PracticeAttempts_CategoryId");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("IX_PracticeAttempts_Status");

            builder.HasIndex(x => new { x.UserId, x.StartedAt })
                .HasDatabaseName("IX_PracticeAttempts_User_StartedAt");

            builder.HasIndex(x => new { x.CategoryId, x.Score })
                .HasDatabaseName("IX_PracticeAttempts_Category_Score");

            // ==============================
            // Relationships
            // ==============================

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
}