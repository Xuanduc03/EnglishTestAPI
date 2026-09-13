

namespace App.Domain.Entities
{
    /// <summary>
    /// Để xây dựng chức năng Leaderboard
    /// </summary>
    public class UserStatistics : BaseEntity
    {
        public Guid UserId { get; set; } //  lưu thông tin cơ bản (name)
        public int TotalExamsCompleted { get; set; }
        public decimal AverageScore { get; set; }
        public int CurrentStreak { get; set; }
        public DateTime? LastActivityDate { get; set; }
        // Navigation
        public virtual User User { get; set; }
    }
}
