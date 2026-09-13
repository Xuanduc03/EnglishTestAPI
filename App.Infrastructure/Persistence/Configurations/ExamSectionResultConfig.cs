using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations
{
    public class ExamSectionResultConfig : IEntityTypeConfiguration<ExamSectionResult>
    {
        public void Configure(EntityTypeBuilder<ExamSectionResult> builder)
        {
            // Table
            builder.ToTable("exam_section_results");

            // Primary key
            builder.HasKey(e => e.Id);

            // Columns
            builder.Property(e => e.TotalQuestions)
                .IsRequired();

            builder.Property(e => e.CorrectAnswers)
                .IsRequired();

            builder.Property(e => e.ConvertedScore);

            // FK -> ExamAttempt
            builder.HasOne(e => e.Attempt)
                .WithMany(a => a.SectionResults)
                .HasForeignKey(e => e.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index:
            // 1 Attempt chỉ có 1 result cho mỗi Section
            builder.HasIndex(e => new
            {
                e.ExamAttemptId,
                e.ExamSectionId
            })
            .IsUnique();
        }
    }
}