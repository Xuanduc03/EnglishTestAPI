using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    // ============================================
    // PRACTICE ANSWER - Chi tiết từng câu trả lời
    // ============================================

    /// <summary>
    /// Lưu chi tiết câu trả lời của từng question trong practice session
    /// Tương tự StudentAnswer nhưng cho Practice
    /// </summary>
    public class PracticeAnswer : BaseEntity
    {
        public Guid PracticeAttemptId { get; set; }
        public virtual PracticeAttempt PracticeAttempt { get; set; }

        public Guid QuestionId { get; set; }
        public virtual Question Question { get; set; }

        public Guid? SelectedAnswerId { get; set; }
        public virtual Answer SelectedAnswer { get; set; }

        // ============================================
        // ANSWER STATE
        // ============================================

        public bool IsCorrect { get; set; }
        public bool IsMarkedForReview { get; set; }  // Đánh dấu để review lại

        // ============================================
        // TIMING & ORDER
        // ============================================

        public DateTime? AnsweredAt { get; set; }
        public int TimeSpentSeconds { get; set; }  // Thời gian làm câu này
        public int OrderIndex { get; set; }  // Question number in session (1, 2, 3...)

        // ============================================
        // METADATA
        // ============================================

        public int? ChangeCount { get; set; }  // Số lần đổi đáp án (analytics)

        // ============================================
        // COMPUTED
        // ============================================

        [NotMapped]
        public bool IsAnswered => SelectedAnswerId.HasValue;
    }

}
