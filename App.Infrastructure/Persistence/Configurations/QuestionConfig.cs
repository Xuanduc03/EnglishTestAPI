using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class QuestionConfig : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.ToTable("questions");

            builder.HasKey(x => x.Id);

            // ==============================
            // Content
            // ==============================

            builder.Property(x => x.Content)
                .HasColumnType("longtext");

            builder.Property(x => x.Explanation)
                .HasColumnType("longtext");

            builder.Property(x => x.MetadataJson)
                .HasColumnType("longtext");

            builder.Property(x => x.RubricJson)
                .HasColumnType("longtext");

            builder.Property(x => x.AiPromptTemplate)
                .HasColumnType("longtext");

            builder.Property(x => x.SampleAnswer)
                .HasColumnType("longtext");

            // ==============================
            // Question Type
            // ==============================

            builder.Property(x => x.QuestionTypeId)
                .IsRequired();

            builder.Ignore(x => x.QuestionType);

            // ==============================
            // Prompt Type
            // ==============================

            builder.Property(x => x.PromptTypeId);

            builder.Ignore(x => x.PromptType);

            // ==============================
            // Properties
            // ==============================

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.IsPublic)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.OrderIndex)
                .IsRequired();

            builder.Property(x => x.TimeLimitSeconds);

            builder.Property(x => x.DefaultScore)
                .IsRequired();

            builder.Property(x => x.ShuffleAnswers)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.IsAiGraded)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.MinWords);

            builder.Property(x => x.MaxWords);

            // ==============================
            // Indexes
            // ==============================

            builder.HasIndex(x => x.CategoryId)
                .HasDatabaseName("IX_Questions_CategoryId");

            builder.HasIndex(x => x.GroupId)
                .HasDatabaseName("IX_Questions_GroupId");

            builder.HasIndex(x => x.DifficultyId)
                .HasDatabaseName("IX_Questions_DifficultyId");

            builder.HasIndex(x => x.QuestionTypeId)
                .HasDatabaseName("IX_Questions_QuestionTypeId");

            builder.HasIndex(x => new
            {
                x.GroupId,
                x.OrderIndex
            })
            .HasDatabaseName("IX_Questions_Group_OrderIndex");

            builder.HasIndex(x => new
            {
                x.CategoryId,
                x.IsActive,
                x.IsPublic
            })
            .HasDatabaseName("IX_Questions_Category_Active_Public");

            // ==============================
            // Relationships
            // ==============================

            // Question -> Category
            builder.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Question -> Difficulty
            builder.HasOne(x => x.Difficulty)
                .WithMany()
                .HasForeignKey(x => x.DifficultyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Question -> QuestionGroup
            builder.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Question -> Answers
            builder.HasMany(x => x.Answers)
                .WithOne(x => x.Question)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question -> Media
            builder.HasMany(x => x.Media)
                .WithOne(x => x.Question)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Question -> Tags
            builder.HasMany(x => x.Tags)
                .WithOne(x => x.Question)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}