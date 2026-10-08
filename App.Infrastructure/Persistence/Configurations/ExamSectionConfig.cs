using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class ExamSectionConfig : IEntityTypeConfiguration<ExamSection>
    {
        public void Configure(EntityTypeBuilder<ExamSection> builder)
        {
            builder.ToTable("exam_sections");

            builder.HasKey(x => x.Id);

            // ==============================
            // Properties
            // ==============================

            builder.Property(x => x.Instructions)
                .HasMaxLength(2000);

            builder.Property(x => x.OrderIndex)
                .IsRequired();

            builder.Property(x => x.TimeLimit);

            // ==============================
            // Indexes
            // ==============================

            builder.HasIndex(x => x.ExamId);

            builder.HasIndex(x => x.CategoryId);

            builder.HasIndex(x => new
            {
                x.ExamId,
                x.OrderIndex
            })
            .IsUnique();

            // ==============================
            // Relationships
            // ==============================

            // Exam -> ExamSections
            builder.HasOne(x => x.Exam)
                .WithMany(x => x.Sections)
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Category -> ExamSections
            builder.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExamSection -> ExamQuestions
            builder.HasMany(x => x.ExamQuestions)
                .WithOne(x => x.ExamSection)
                .HasForeignKey(x => x.ExamSectionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}