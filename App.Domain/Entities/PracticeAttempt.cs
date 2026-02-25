using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{
    /// <summary>
    /// Practice Attempt - Session luyện tập của học sinh
    /// Một session có thể luyện 1 hoặc nhiều Part
    /// </summary>
    public class PracticeAttempt : BaseEntity
    {
        // ============================================
        // BASIC INFO
        // ============================================

        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; }  //   Fix typo, nullable (multi-part practice)

        public string Title { get; set; } // "Part 5 Practice", "Reading Practice"

        // ============================================
        // TIMING
        // ============================================

        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? TimeLimitSeconds { get; set; }  // NULL = no time limit

        public int? ActualTimeSeconds { get; set; }

        // ============================================
        // STATUS
        // ============================================

        public AttemptStatus Status { get; set; }

        // ============================================
        // RESULTS SUMMARY
        // ============================================

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int UnansweredQuestions { get; set; }

        public double Score { get; set; }  // Điểm số (0-100 hoặc 0-990 TOEIC)
        public double AccuracyPercentage { get; set; }  // % đúng

        // ============================================
        // METADATA
        // ============================================

        public bool IsRandomOrder { get; set; }  // Có random câu hỏi không
        public string? Notes { get; set; }  // Ghi chú của học sinh

        // ============================================
        // NAVIGATION PROPERTIES
        // ============================================

        public virtual User User { get; set; }
        public virtual Category Category { get; set; }

        //   Chi tiết từng câu trả lời
        public virtual ICollection<PracticeAnswer> Answers { get; set; } = new List<PracticeAnswer>();

        //   Kết quả từng Part (nếu practice nhiều part)
        public virtual ICollection<PracticePartResult> PartResults { get; set; } = new List<PracticePartResult>();

        // ============================================
        // COMPUTED PROPERTIES
        // ============================================

        [NotMapped]
        public bool IsTimedOut => TimeLimitSeconds.HasValue
            && ActualTimeSeconds.HasValue
            && ActualTimeSeconds.Value > TimeLimitSeconds.Value;

        [NotMapped]
        public int AnsweredQuestions => CorrectAnswers + IncorrectAnswers;

        [NotMapped]
        public double CompletionPercentage => TotalQuestions > 0
            ? (double)AnsweredQuestions / TotalQuestions * 100
            : 0;
    }

   
    

    // ============================================
    // ENUMS
    // ============================================

    public enum AttemptStatus
    {
        InProgress = 0,   //   Fix typo
        Submitted = 1,
        Abandoned = 2,    //   Thêm: Bỏ giữa chừng
        TimedOut = 3      //   Thêm: Hết giờ
    }

    // ============================================
    // EXTENSION METHODS
    // ============================================

    public static class PracticeAttemptExtensions
    {
        /// <summary>
        /// Calculate score from correct answers
        /// </summary>
        public static void CalculateScore(this PracticeAttempt attempt)
        {
            if (attempt.TotalQuestions == 0)
            {
                attempt.Score = 0;
                attempt.AccuracyPercentage = 0;
                return;
            }

            // Basic percentage
            attempt.AccuracyPercentage = Math.Round(
                (double)attempt.CorrectAnswers / attempt.TotalQuestions * 100,
                2
            );

            // TOEIC scoring (simplified)
            // Real TOEIC uses IRT, this is approximation
            attempt.Score = Math.Round(
                attempt.AccuracyPercentage * 9.9,  // 100% = 990
                0
            );
        }

        /// <summary>
        /// Check if attempt can be submitted
        /// </summary>
        public static bool CanSubmit(this PracticeAttempt attempt)
        {
            return attempt.Status == AttemptStatus.InProgress
                && attempt.TotalQuestions > 0;
        }

        /// <summary>
        /// Mark as submitted and calculate final score
        /// </summary>
        public static void Submit(this PracticeAttempt attempt)
        {
            if (!attempt.CanSubmit())
                throw new InvalidOperationException("Cannot submit this attempt");

            attempt.SubmittedAt = DateTime.UtcNow;

            // Check timeout
            if (attempt.IsTimedOut)
            {
                attempt.Status = AttemptStatus.TimedOut;
            }
            else
            {
                attempt.Status = AttemptStatus.Submitted;
            }

            // Calculate counts
            attempt.CorrectAnswers = attempt.Answers.Count(a => a.IsCorrect);
            attempt.IncorrectAnswers = attempt.Answers.Count(a => a.IsAnswered && !a.IsCorrect);
            attempt.UnansweredQuestions = attempt.Answers.Count(a => !a.IsAnswered);

            // Calculate score
            attempt.CalculateScore();
        }
    }
}