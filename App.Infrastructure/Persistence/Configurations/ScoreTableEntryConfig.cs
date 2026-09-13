using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class ScoreTableEntryConfig
        : IEntityTypeConfiguration<ScoreTableEntry>
    {
        public void Configure(EntityTypeBuilder<ScoreTableEntry> builder)
        {
            builder.ToTable("score_table_entries");

            builder.HasKey(x => x.Id);

            // ==============================
            // Properties
            // ==============================

            builder.Property(x => x.CorrectAnswers)
                .IsRequired();

            builder.Property(x => x.Score)
                .IsRequired();

            // ==============================
            // Indexes
            // ==============================

            // Một ScoreTable không được có
            // 2 entry cùng số câu đúng
            builder.HasIndex(x => new
            {
                x.ScoreTableId,
                x.CorrectAnswers
            })
            .IsUnique();

            // ==============================
            // Relationships
            // ==============================

            builder.HasOne(x => x.ScoreTable)
                .WithMany(x => x.Entries)
                .HasForeignKey(x => x.ScoreTableId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}