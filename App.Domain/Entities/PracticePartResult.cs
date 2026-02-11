using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    // ============================================
    // PRACTICE PART RESULT - Kết quả từng Part
    // ============================================

    /// <summary>
    /// Kết quả chi tiết từng Part trong practice session
    /// Ví dụ: Practice Part 5 + Part 6 → 2 records
    /// </summary>
    public class PracticePartResult : BaseEntity
    {
        public Guid PracticeAttemptId { get; set; }
        public virtual PracticeAttempt PracticeAttempt { get; set; }

        public Guid CategoryId { get; set; }
        public virtual Category Category { get; set; }

        // ============================================
        // PART INFO
        // ============================================

        public int PartNumber { get; set; }  // 1-7 for TOEIC
        public string PartName { get; set; }  // "Part 1", "Part 5"...

        // ============================================
        // RESULTS
        // ============================================

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int UnansweredQuestions { get; set; }

        public double Percentage { get; set; }  // Accuracy %
        public int TotalTimeSeconds { get; set; }

        // ============================================
        // COMPUTED
        // ============================================

        [NotMapped]
        public double AverageTimePerQuestion => TotalQuestions > 0
            ? (double)TotalTimeSeconds / TotalQuestions
            : 0;
    }
}
