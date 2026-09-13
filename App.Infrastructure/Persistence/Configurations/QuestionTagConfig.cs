using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations
{
    public class QuestionTagConfig : IEntityTypeConfiguration<QuestionTag>
    {
        public void Configure(EntityTypeBuilder<QuestionTag> builder)
        {
            builder.ToTable("question_tags");

            builder.HasKey(t => t.Id);

            // Tag
            builder.Property(t => t.Tag)
                .IsRequired()
                .HasMaxLength(100);

            // Tag type
            builder.Property(t => t.TagType)
                .HasMaxLength(50);

            // Indexes
            builder.HasIndex(t => t.Tag);

            builder.HasIndex(t => t.TagType);

            // Question relationship
            builder.HasOne(t => t.Question)
                .WithMany(q => q.Tags)
                .HasForeignKey(t => t.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuestionGroup relationship
            builder.HasOne(t => t.QuestionGroup)
                .WithMany(g => g.Tags)
                .HasForeignKey(t => t.QuestionGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}