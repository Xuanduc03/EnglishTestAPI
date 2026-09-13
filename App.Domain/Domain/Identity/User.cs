using App.Domain.Identity;

namespace App.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string? PasswordHash { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;

        public UserRole Role { get; set; } = UserRole.Student;
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public virtual Student? StudentProfile { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }
}