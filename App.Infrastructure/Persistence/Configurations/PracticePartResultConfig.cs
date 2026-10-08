using App.Domain.Domain.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class PracticePartResultConfig
        : IEntityTypeConfiguration<PracticePartResult>
    {
        public void Configure(EntityTypeBuilder<PracticePartResult> builder)
        {
            builder.ToTable("practice_part_results");

            builder.HasKey(x => x.Id);

            // ==============================
            // Properties
            // ==============================

            builder.Property(x => x.PartNumber)
                .IsRequired();

            builder.Property(x => x.PartName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Percentage)
                .HasPrecision(5, 2);

            // ==============================
            // Indexes
            // ==============================

            builder.HasIndex(x => x.PracticeAttemptId)
                .HasDatabaseName("IX_PracticePartResults_AttemptId");

            builder.HasIndex(x => new
            {
                x.PracticeAttemptId,
                x.PartNumber
            })
            .HasDatabaseName("IX_PracticePartResults_Attempt_PartNumber");

            // ==============================
            // Relationships
            // ==============================

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