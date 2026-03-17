using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    namespace App.Application.DTOs
    {
        // ============================================================
        // TOEIC WRITING — DTOs
        // Part 1 (Q1-8) : Write a sentence from a photo + 2 words
        // Part 2 (Q9-10): Respond to a written request (email)
        // Part 3 (Q11)  : Write an opinion essay
        // ============================================================

        // ==================== SESSION ====================

        public class WritingSessionDto
        {
            public Guid SessionId { get; set; }
            public string Title { get; set; } = string.Empty;
            public int TotalQuestions { get; set; }
            public int? TimeLimitSeconds { get; set; }       // 60 phút = 3600s
            public List<WritingPartDto> Parts { get; set; } = new();
        }

        public class WritingPartDto
        {
            public Guid PartId { get; set; }
            public string PartName { get; set; } = string.Empty;  // "Writing Part 1"
            public int PartNumber { get; set; }                    // 1, 2, 3
            public string Description { get; set; } = string.Empty;
            public string Instructions { get; set; } = string.Empty; // Hiển thị hướng dẫn đầu part
            public int TimeLimitSeconds { get; set; }             // P1=300s, P2=600s, P3=1200s
            public List<WritingQuestionDto> Questions { get; set; } = new();
        }

        public class WritingQuestionDto
        {
            public Guid QuestionId { get; set; }
            public int OrderIndex { get; set; }       // Thứ tự trong part (1-based)
            public int QuestionNumber { get; set; }   // Số tổng (1-11)
            public WritingPartType PartType { get; set; }

            // ── Part 1: Sentence from photo ──
            public string? ImageUrl { get; set; }
            public List<string> RequiredWords { get; set; } = new(); // 2 từ bắt buộc dùng

            // ── Part 2: Email response ──
            public string? EmailContent { get; set; }    // Nội dung email cần reply
            public List<string> ResponsePoints { get; set; } = new(); // 3 điểm cần đề cập

            // ── Part 3: Essay ──
            public string? EssayPrompt { get; set; }     // Câu hỏi / đề bài
            public int? MinWords { get; set; }            // 300 words
            public int? MaxWords { get; set; }

            // ── Shared ──
            public string Content { get; set; } = string.Empty; // Đề bài chính
            public string? TextAnswer { get; set; }      // Đáp án user (load lại khi resume)
            public WritingAnswerStatus AnswerStatus { get; set; } = WritingAnswerStatus.NotStarted;
            public WritingGradingResultDto? GradingResult { get; set; } // null khi chưa nộp
        }

        // ==================== SUBMIT ====================

        public class SubmitWritingAnswerRequest
        {
            public Guid SessionId { get; set; }
            public Guid QuestionId { get; set; }
            public string TextAnswer { get; set; } = string.Empty;
            public int TimeSpentSeconds { get; set; }
        }

        /// <summary>
        /// Nộp toàn bộ bài Writing khi hết giờ hoặc bấm Submit all
        /// </summary>
        public class SubmitWritingSessionRequest
        {
            public Guid SessionId { get; set; }
            public List<WritingAnswerItem> Answers { get; set; } = new();
            public int TotalTimeSeconds { get; set; }
        }

        public class WritingAnswerItem
        {
            public Guid QuestionId { get; set; }
            public string TextAnswer { get; set; } = string.Empty;
            public int TimeSpentSeconds { get; set; }
        }

        // ==================== RESULT ====================

        public class WritingSessionResultDto
        {
            public Guid SessionId { get; set; }
            public string Title { get; set; } = string.Empty;
            public int TotalQuestions { get; set; }
            public int GradedQuestions { get; set; }
            public bool IsFullyGraded { get; set; }
            public double? OverallScore { get; set; }    // 0-200 điểm TOEIC Writing
            public int TotalTimeSeconds { get; set; }
            public List<WritingPartResultDto> PartResults { get; set; } = new();
        }

        public class WritingPartResultDto
        {
            public string PartName { get; set; } = string.Empty;
            public int PartNumber { get; set; }
            public int TotalQuestions { get; set; }
            public int GradedQuestions { get; set; }
            public double? AverageScore { get; set; }
            public List<WritingQuestionResultDto> QuestionResults { get; set; } = new();
        }

        public class WritingQuestionResultDto
        {
            public Guid QuestionId { get; set; }
            public int QuestionNumber { get; set; }
            public string TextAnswer { get; set; } = string.Empty;
            public WritingGradingResultDto? GradingResult { get; set; }
            public WritingGradingStatus GradingStatus { get; set; }
        }

        /// <summary>
        /// Kết quả chấm từ IAIGradingService — map từ AIGradingResult
        /// </summary>
        public class WritingGradingResultDto
        {
            public double Score { get; set; }           // 0-5 cho P1, 0-4 cho P2/P3 rubric level
            public string Feedback { get; set; } = string.Empty;
            public WritingScoreBreakdown? Breakdown { get; set; }
        }

        /// <summary>
        /// Chi tiết điểm theo tiêu chí — deserialize từ AiScoreDetailJson
        /// </summary>
        public class WritingScoreBreakdown
        {
            // Part 1
            public double? GrammarScore { get; set; }
            public double? RelevanceScore { get; set; }       // Đúng nội dung ảnh
            public double? RequiredWordsUsed { get; set; }    // Dùng đủ 2 từ bắt buộc

            // Part 2
            public double? ContentScore { get; set; }         // Đủ 3 điểm yêu cầu
            public double? OrganizationScore { get; set; }
            public double? VocabularyScore { get; set; }

            // Part 3
            public double? ArgumentScore { get; set; }        // Lập luận rõ ràng
            public double? CoherenceScore { get; set; }
            public double? WordCount { get; set; }
        }

        // ==================== REVIEW ====================

        public class WritingReviewDto
        {
            public Guid SessionId { get; set; }
            public string Title { get; set; } = string.Empty;
            public List<WritingQuestionResultDto> Questions { get; set; } = new();
        }

        // ==================== IN-PROGRESS ====================

        public class InProgressWritingDto
        {
            public Guid AttemptId { get; set; }
            public string Title { get; set; } = string.Empty;
            public double Progress { get; set; }        // % câu đã trả lời
            public DateTime StartedAt { get; set; }
            public DateTime LastUpdated { get; set; }
            public int? TimeLimitSeconds { get; set; }
            public int? ElapsedSeconds { get; set; }
        }

        // ==================== ENUMS ====================

        public enum WritingPartType
        {
            SentenceFromPhoto = 1,  // Part 1
            EmailResponse = 2,      // Part 2
            OpinionEssay = 3        // Part 3
        }

        public enum WritingAnswerStatus
        {
            NotStarted,
            Draft,       // Đã gõ nhưng chưa submit part
            Submitted
        }

        public enum WritingGradingStatus
        {
            Pending,
            Grading,
            Completed,
            Failed
        }
    }
}
