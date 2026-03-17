// ============================================
// FILE: App.Domain/Entities/PracticePartResult.cs
// ============================================
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{
    public class PracticePartResult : BaseEntity
    {
        public Guid PracticeAttemptId { get; set; }
        public virtual PracticeAttempt PracticeAttempt { get; set; }

        public Guid CategoryId { get; set; }
        public virtual Category Category { get; set; }

        // ============================================
        // PART INFO
        // ============================================
        public int PartNumber { get; set; }   // 1-7 TOEIC L/R | 1-3 Writing | 1-3 Speaking
        public string PartName { get; set; }  // "Part 1", "Writing Part 2"...

        // ============================================
        // RESULTS — Multiple Choice (Practice)
        // ============================================
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int UnansweredQuestions { get; set; }
        public double Percentage { get; set; }    // Accuracy %

        // ============================================
        // RESULTS — AI Graded (Writing + Speaking)
        // NULL khi chưa chấm xong
        // ============================================
        public double? AverageAiScore { get; set; }       // Điểm trung bình AI các câu trong part
        public int PendingGradingCount { get; set; } = 0; // Số câu còn chờ AI chấm
        public int FailedGradingCount { get; set; } = 0;  // Số câu AI chấm thất bại

        // ============================================
        // TIMING
        // ============================================
        public int TotalTimeSeconds { get; set; }

        // ============================================
        // COMPUTED
        // ============================================
        [NotMapped]
        public double AverageTimePerQuestion =>
            TotalQuestions > 0 ? (double)TotalTimeSeconds / TotalQuestions : 0;

        [NotMapped]
        public bool IsFullyGraded =>
            PendingGradingCount == 0 && AverageAiScore.HasValue;
    }
}