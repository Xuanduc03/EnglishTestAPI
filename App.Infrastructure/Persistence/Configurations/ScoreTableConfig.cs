using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class ScoreTableConfig : IEntityTypeConfiguration<ScoreTable>
    {
        public void Configure(EntityTypeBuilder<ScoreTable> builder)
        {
            builder.ToTable("score_tables");

            builder.HasKey(x => x.Id);

            // ==============================
            // Relationships
            // ==============================

            // ScoreTable -> Skill Category
            // Ví dụ: LISTENING / READING
            builder.HasOne(x => x.SkillCategory)
                .WithMany()
                .HasForeignKey(x => x.SkillCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==============================
            // Indexes
            // ==============================

            builder.HasIndex(x => x.SkillCategoryId)
                .HasDatabaseName("IX_ScoreTables_SkillCategoryId");
        }
    }
}