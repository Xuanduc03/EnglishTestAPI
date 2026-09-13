using App.Domain.Entities;

namespace App.Domain.Identity
{
    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;
        public string TokenHash { get; set; } = default!;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Thông tin phục vụ audit / phát hiện bất thường
        public string? CreatedByIp { get; set; }
        public string? UserAgent { get; set; }

        // Revoke thủ công (logout, đổi mật khẩu, phát hiện bị đánh cắp...)
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }

        // Token rotation: token cũ trỏ sang token mới thay thế nó
        public string? ReplacedByTokenHash { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}