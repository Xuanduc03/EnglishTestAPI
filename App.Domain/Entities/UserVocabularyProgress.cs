using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // Navigation
        public virtual User User { get; set; }
        public virtual VocabularyWord Word { get; set; }
    }
}
