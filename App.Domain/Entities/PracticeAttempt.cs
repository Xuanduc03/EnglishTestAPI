using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{
    public class PracticeAttempt : BaseEntity
    {
        // ============================================
        // BASIC INFO
        // ============================================
        public Guid UserId { get; set; }
        public Guid? CategoryId { get; set; }
        public string Title { get; set; }
        public AttemptType AttemptType { get; set; } = AttemptType.Practice;

        // ============================================
        // TIMING
        // ============================================
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int? TimeLimitSeconds { get; set; }
        public int? ActualTimeSeconds { get; set; }

        public int? TotalTimeSeconds { get; set; }

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
        public double Score { get; set; }
        public double AccuracyPercentage { get; set; }

        // ============================================
        // METADATA
        // ============================================
        public bool IsRandomOrder { get; set; }
        public string? Notes { get; set; }

        // ============================================
        // NAVIGATION PROPERTIES
        // ============================================
        public virtual User User { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<PracticeAnswer> Answers { get; set; } = new List<PracticeAnswer>();
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

    public enum AttemptType
    {
        Practice = 0,   // Listening + Reading (multiple choice)
        Writing = 1,   // Writing Part 1/2/3 (open text, AI graded)
        Speaking = 2
    }

    public enum AttemptStatus
    {
        InProgress = 0,
        Submitted = 1,   // Practice dùng
        Completed = 2,
        Abandoned = 3,
        TimedOut = 4
    }
}