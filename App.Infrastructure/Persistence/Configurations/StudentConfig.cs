using App.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Persistence.Configurations
{
    public class StudentConfig : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("students");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Gender);

            builder.Property(s => s.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(s => s.AvatarPublicId)
                .HasMaxLength(255);

            builder.Property(s => s.Streak)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(s => s.Points)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(s => s.MemberLevel)
                .IsRequired()
                .HasMaxLength(50);


            builder.HasOne(s => s.User)
               .WithOne(u => u.StudentProfile)
               .HasForeignKey<Student>(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);


            // Student -> ExamAttempts
            builder.HasMany(s => s.ExamAttempts)
                .WithOne(e => e.Student)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
