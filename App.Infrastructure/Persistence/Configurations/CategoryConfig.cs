using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using App.Domain.Entities;

namespace App.Infrastructure.Persistence.Configurations
{
    public class CategoryConfig : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories");

            builder.HasKey(c => c.Id);

            builder.HasIndex(c => c.Code)
                .IsUnique(false);

            builder.HasIndex(c => c.CodeType);

            builder.HasOne(c => c.Parent)
                     .WithMany(c => c.Children)
                     .HasForeignKey(c => c.ParentId)
                     .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.CodeType, c.ParentId });

            builder.HasIndex(c => c.Code)
                .HasDatabaseName("IX_Category_Code")
                .IsUnique(false);
        }
    }
}
