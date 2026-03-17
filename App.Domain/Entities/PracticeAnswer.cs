
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{
    public class PracticeAnswer : BaseEntity
    {
        public Guid PracticeAttemptId { get; set; }
        public virtual PracticeAttempt PracticeAttempt { get; set; }

        public Guid QuestionId { get; set; }
        public virtual Question Question { get; set; }

        // ============================================
        // MULTIPLE CHOICE (Practice: Listening/Reading)
        // ============================================
        public Guid? SelectedAnswerId { get; set; }
        public virtual Answer? SelectedAnswer { get; set; }

        // ============================================
        // WRITING (open text)
        // ============================================
        public string? TextAnswer { get; set; }
        public int? WordCount { get; set; }

        // ============================================
        // SPEAKING (audio recording)
        // ============================================
        public string? AudioUrl { get; set; }
        public string? AudioPublicId { get; set; }           // Cloudinary public_id để delete
        public int? RecordingDurationSeconds { get; set; }   // Độ dài bản ghi âm

        // ============================================
        // ANSWER STATE
        // ============================================
        public bool IsCorrect { get; set; }
        public bool IsMarkedForReview { get; set; }

        // ============================================
        // TIMING & ORDER
        // ============================================
        public DateTime? AnsweredAt { get; set; }
        public int TimeSpentSeconds { get; set; }
        public int OrderIndex { get; set; }

        // ============================================
        // METADATA
        // ============================================
        public int? ChangeCount { get; set; }   // Số lần đổi đáp án

        // ============================================
        // AI GRADING (Writing + Speaking)
        // ============================================
        public GradingStatusEnum GradingStatus { get; set; } = GradingStatusEnum.NotRequired;
        public bool IsAiGraded { get; set; } = false;
        public DateTime? GradedAt { get; set; }

        // Score tổng do AI chấm (0-5 Writing P1, 0-4 W P2/P3, 0-5 Speaking)
        // Lưu riêng để query nhanh, không phải parse JSON
        public double? AiScore { get; set; }

        public string? AiFeedback { get; set; }
        public string? AiScoreDetailJson { get; set; }   // JSON chi tiết rubric breakdown

        // ============================================
        // COMPUTED
        // ============================================
        [NotMapped]
        public bool IsAnswered =>
            SelectedAnswerId.HasValue
            || !string.IsNullOrWhiteSpace(TextAnswer)
            || !string.IsNullOrWhiteSpace(AudioUrl);

        [NotMapped]
        public bool NeedsAiGrading =>
            GradingStatus == GradingStatusEnum.Pending
            || GradingStatus == GradingStatusEnum.Grading;
    }

    public enum GradingStatusEnum
    {
        NotRequired = 0,   // Multiple choice — không cần AI
        Pending = 1,   // Chờ enqueue / chờ job chạy
        Grading = 2,   // Hangfire job đang chạy
        Completed = 3,   // Đã chấm xong, có AiScore
        Failed = 4    // AI lỗi — Hangfire sẽ retry
    }
    /// <summary>
    /// Lưu trong AiScoreDetailJson của PracticeAnswer khi Speaking
    /// </summary>
    public class SpeakingScoreDetail
    {
        public double PronunciationScore { get; set; }  // 0-5: âm chuẩn, stress, intonation
        public double FluencyScore { get; set; }         // 0-5: tốc độ, không ngập ngừng
        public double ContentScore { get; set; }         // 0-5: trả lời đúng câu hỏi
        public double VocabularyScore { get; set; }      // 0-5: từ vựng phong phú
        public double GrammarScore { get; set; }         // 0-5: ngữ pháp
        public string? TranscribedText { get; set; }     // AI transcribe audio thành text
    }


}