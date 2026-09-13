using App.Domain.Entities;

namespace App.Domain.Identity
{
    public class PasswordResetToken : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public string TokenHash { get; set; } = default!; // hash, không lưu plain text
        public DateTime ExpiresAt { get; set; }            // nên ngắn, ví dụ 15-30 phút
        public DateTime? UsedAt { get; set; }               // token dùng 1 lần
        public string? RequestedByIp { get; set; }  
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsUsed => UsedAt != null;
        public bool IsValid => !IsExpired && !IsUsed;
    }
}