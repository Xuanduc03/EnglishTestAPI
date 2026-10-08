using App.Domain.Domain.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class ExamLogConfig : IEntityTypeConfiguration<ExamActivityLog>
    {
        public void Configure(EntityTypeBuilder<ExamActivityLog> entity)
        {
            entity.ToTable("exam_activity_logs");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new
            {
                e.ExamAttemptId,
                e.OccurredAt
            });

            entity.HasIndex(e => new
            {
                e.UserId,
                e.OccurredAt
            });

            entity.HasIndex(e => new
            {
                e.QuestionId,
                e.OccurredAt
            });

            entity.Property(e => e.Action)
                .HasConversion<int>();

            entity.Property(e => e.MetadataJson)
                .HasColumnType("json");

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.HasOne(e => e.ExamAttempt)
                .WithMany()
                .HasForeignKey(e => e.ExamAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Question)
                .WithMany()
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.SetNull);
        
        }
    }
}
