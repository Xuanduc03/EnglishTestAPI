using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class ExamConfig : IEntityTypeConfiguration<Exam>
    {
        public void Configure(EntityTypeBuilder<Exam> builder)
        {
            builder.ToTable("exams");

            builder.HasKey(e => e.Id);

            // ==============================
            // Basic information
            // ==============================

            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(e => e.Code)
                .IsUnique();

            builder.Property(e => e.Description)
                .HasMaxLength(2000);

            // ==============================
            // Time & Score
            // ==============================

            builder.Property(e => e.Duration)
                .IsRequired();

            builder.Property(e => e.TotalScore)
                .HasPrecision(10, 2)
                .IsRequired();

            builder.Property(e => e.StartDate);

            builder.Property(e => e.EndDate);

            // ==============================
            // Exam classification
            // ==============================

            builder.Property(e => e.Type)
                .IsRequired();

            builder.Property(e => e.Category)
                .IsRequired();

            builder.Property(e => e.Scope)
                .IsRequired();

            builder.Property(e => e.Level)
                .IsRequired();

            // ==============================
            // Tags & Metadata
            // ==============================

            builder.Property(e => e.Tags);

            builder.Property(e => e.MetaData);

            // ==============================
            // Status & Settings
            // ==============================

            builder.Property(e => e.Status)
                .IsRequired()
                .HasDefaultValue(ExamStatus.Draft);

            builder.Property(e => e.ShuffleQuestions)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.ShuffleAnswers)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.Version)
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // ==============================
            // Exam -> Sections (1 - N)
            // Xóa Exam -> xóa Sections
            // ==============================

            builder.HasMany(e => e.Sections)
                .WithOne(s => s.Exam)
                .HasForeignKey(s => s.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==============================
            // Exam -> ExamAttempts (1 - N)
            // ==============================

            builder.HasMany(e => e.Attempts)
                .WithOne(a => a.Exam)
                .HasForeignKey(a => a.ExamId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}