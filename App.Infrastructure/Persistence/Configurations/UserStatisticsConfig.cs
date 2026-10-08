using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence.Configurations
{
    public class UserStatisticsConfig : IEntityTypeConfiguration<UserStatistics>
    {
        public void Configure(EntityTypeBuilder<UserStatistics> builder)
        {
            // Table
            builder.ToTable("user_statistics");

            // Primary key
            builder.HasKey(e => e.Id);

            // UserId unique:
            // Mỗi User chỉ có một UserStatistics
            builder.HasIndex(e => e.UserId)
                .IsUnique();

            // User relationship: 1 User - 1 UserStatistics
            builder.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<UserStatistics>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}