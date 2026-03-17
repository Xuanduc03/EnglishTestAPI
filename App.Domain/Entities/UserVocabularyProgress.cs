

namespace App.Domain.Entities
{
    /// <summary>
    /// Lưu trữ quá trình học từ vựng của người dùng
    /// </summary>
    public class UserVocabularyProgress : BaseEntity
    {
        public Guid UserId { get; set; }
        public Guid WordId { get; set; }
        public string Status { get; set; } = "learning"; // learning, known, mastered
        public int TimesReviewed { get; set; }
        public int TimesCorrect { get; set; }
        public DateTime? LastReviewedAt { get; set; }
        public DateTime? NextReviewAt { get; set; }
        public int Interval { get; set; } = 1;        // Số ngày đến lần ôn tiếp
        public int RepetitionCount { get; set; } = 0; // Số lần trả lời đúng liên tiếp
        public double EaseFactor { get; set; } = 2.5; // Hệ số dễ (2.5 = default SM-2)
        // Navigation
        public virtual User User { get; set; }
        public virtual VocabularyWord Word { get; set; }
    }
}
