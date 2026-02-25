

namespace App.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; }
        public string? Password { get; set; }
        public string Fullname {  get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AvatarPublicId { get; set; }
        // Account status
        public bool IsActive { get; set; } = true;
        public bool EmailVerified { get; set; } = true; 

        // Lockout
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }

        // Optional
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public DateTime? LastLogin { get; set; }

        //navigation 
        public virtual Student? StudentProfile { get; set; }
        public virtual ICollection<ExamAttempt> ExamAttempts { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; }

    }
}
