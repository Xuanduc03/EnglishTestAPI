using App.Domain.Identity;

namespace App.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; private set; } = default!;
        public string FullName { get; private set; } = default!;
        public string? PasswordHash { get; private set; }
        public string? Phone { get; private set; }
        public bool IsActive { get; private set; } = true;

        public UserRole Role { get; private set; } = UserRole.Student;
        public int FailedLoginAttempts { get; private set; }
        public DateTime? LockoutEnd { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        public virtual Student? StudentProfile { get; set; }
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

        // EF Core materialization.
        protected User() { }

        public User(string email, string fullName, UserRole role)
        {
            UpdateProfile(email, fullName, null);
            ChangeRole(role);
        }

        public void UpdateProfile(string email, string fullName, string? phone)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email không được để trống", nameof(email));
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Họ tên không được để trống", nameof(fullName));
            if (email.Trim().Length > 255) throw new ArgumentOutOfRangeException(nameof(email));
            if (fullName.Trim().Length > 200) throw new ArgumentOutOfRangeException(nameof(fullName));
            if (phone?.Trim().Length > 20) throw new ArgumentOutOfRangeException(nameof(phone));
            Email = email.Trim().ToLowerInvariant();
            FullName = fullName.Trim();
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        }

        public void ChangePasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Hash mật khẩu không hợp lệ", nameof(passwordHash));
            PasswordHash = passwordHash;
        }

        public void ChangeRole(UserRole role)
        {
            if (!Enum.IsDefined(role)) throw new ArgumentOutOfRangeException(nameof(role));
            Role = role;
        }

        public void Deactivate() => IsActive = false;
        public void Reactivate() => IsActive = true;

        public void RecordLoginSuccess(DateTime? now = null)
        {
            FailedLoginAttempts = 0;
            LockoutEnd = null;
            LastLoginAt = now ?? DateTime.UtcNow;
        }

        public void RecordLoginFailure(int maxAllowedAttempts = 5, TimeSpan? lockoutDuration = null, DateTime? now = null)
        {
            if (maxAllowedAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAllowedAttempts));
            FailedLoginAttempts++;
            if (FailedLoginAttempts >= maxAllowedAttempts)
            {
                LockoutEnd = (now ?? DateTime.UtcNow).Add(lockoutDuration ?? TimeSpan.FromMinutes(15));
            }
        }
        public bool IsLockedOut(DateTime? now = null)
        {
            return LockoutEnd.HasValue && LockoutEnd.Value > (now ?? DateTime.UtcNow);
        }

    }
}
