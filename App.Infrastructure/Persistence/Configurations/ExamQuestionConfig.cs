using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class ExamQuestionConfig : IEntityTypeConfiguration<ExamQuestion>
    {
        public void Configure(EntityTypeBuilder<ExamQuestion> builder)
        {
            builder.ToTable("exam_questions");

            builder.HasKey(x => x.Id);

            // ==============================
            // Properties
            // ==============================

            builder.Property(x => x.QuestionNo)
                .IsRequired();

            builder.Property(x => x.Point)
                .IsRequired()
                .HasPrecision(10, 2);

            builder.Property(x => x.OrderIndex)
                .IsRequired();

            builder.Property(x => x.IsMandatory)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.IsShuffleable)
                .IsRequired()
                .HasDefaultValue(true);

            // ==============================
            // Indexes
            // ==============================

            builder.HasIndex(x => x.ExamId)
                .HasDatabaseName("IX_ExamQuestions_ExamId");

            builder.HasIndex(x => x.ExamSectionId)
                .HasDatabaseName("IX_ExamQuestions_ExamSectionId");

            builder.HasIndex(x => x.QuestionId)
                .HasDatabaseName("IX_ExamQuestions_QuestionId");

            // Không trùng thứ tự câu trong cùng Section
            builder.HasIndex(x => new
            {
                x.ExamSectionId,
                x.OrderIndex
            })
            .IsUnique();

            // Không trùng QuestionNo trong cùng Exam
            builder.HasIndex(x => new
            {
                x.ExamId,
                x.QuestionNo
            })
            .IsUnique();

            // ==============================
            // Relationships
            // ==============================

            // ExamQuestion -> Exam
            builder.HasOne(x => x.Exam)
                .WithMany()
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamSection -> ExamQuestions
            builder.HasOne(x => x.ExamSection)
                .WithMany(x => x.ExamQuestions)
                .HasForeignKey(x => x.ExamSectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question -> ExamQuestions
            // Không xóa Question gốc khi xóa ExamQuestion
            builder.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExamQuestion -> ExamAnswers
            builder.HasMany(x => x.ExamAnswers)
                .WithOne(x => x.ExamQuestions)
                .HasForeignKey(x => x.ExamQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}