using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{

    /// <summary>
    /// Dto : SubmitExamCommand
    /// </summary>
    public class SubmitExamResult
    {
        public Guid AttemptId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public double TotalScore { get; set; }
        public double MaxScore { get; set; }
        public double ScorePercent { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int SkippedAnswers { get; set; }
        public int DurationSeconds { get; set; } // thời gian thực tế làm bài
        public List<PartSummary> PartSummaries { get; set; } = new();
    }

    public class PartSummary
    {
        public string PartName { get; set; }
        public int Total { get; set; }
        public int Correct { get; set; }
        public double Score { get; set; }
    }
    // End Dto SubmitExamCommand

    // DTO: GetExamResultQuery
    public class ExamResultDto
    {
        public Guid AttemptId { get; set; }
        public string ExamTitle { get; set; }
        public string ExamCode { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int DurationSeconds { get; set; }

        // ── Điểm thô (% đúng) ──────────────────────────────────
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int SkippedAnswers { get; set; }
        public double RawScore { get; set; }        // tổng điểm thực tế
        public double MaxScore { get; set; }
        public double ScorePercent { get; set; }    // % câu đúng

        // ── Điểm TOEIC quy đổi (nếu là TOEIC exam) ─────────────
        public bool IsToeic { get; set; }
        public int? ListeningScore { get; set; }    // 5 – 495
        public int? ReadingScore { get; set; }      // 5 – 495
        public int? TotalToeicScore { get; set; }   // 10 – 990

        // ── Kết quả từng section ────────────────────────────────
        public List<SectionResultDto> SectionResults { get; set; } = new();
    }

    public class SectionResultDto
    {
        public string SectionName { get; set; }     // "Listening" | "Reading"
        public string SkillType { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double Score { get; set; }
        public int? ToeicConvertedScore { get; set; }
        public double AccuracyPercent => TotalQuestions > 0
            ? Math.Round((double)CorrectAnswers / TotalQuestions * 100, 1)
            : 0;
    }

    // End: GetEXamResultQuery

    /// DTO: GetExamPreviewQuery 
    /// Xem preview lại bài làm 
    public class ExamReviewDto
    {
        public Guid AttemptId { get; set; }
        public string ExamTitle { get; set; }
        public int TotalQuestions { get; set; }
        public List<ReviewSectionDto> Sections { get; set; } = new();
    }

    public class ReviewSectionDto
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public string SkillType { get; set; }
        public List<ReviewQuestionDto> Questions { get; set; } = new();
    }

    public class ReviewQuestionDto
    {
        public Guid ExamQuestionId { get; set; }
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public string Content { get; set; }
        public string QuestionType { get; set; }
        public bool IsFlagged { get; set; }

        // Media
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }

        // Đáp án (GỬI đủ khi review — đã nộp bài rồi)
        public List<ReviewAnswerOption> Answers { get; set; } = new();

        // Kết quả của user
        public Guid? SelectedAnswerId { get; set; }
        public Guid? CorrectAnswerId { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsAnswered { get; set; }
        public double Point { get; set; }
        public int? TimeSpentSeconds { get; set; }

        // Giải thích (nếu có)
        public string? Explanation { get; set; }
        public string? ExplanationVi { get; set; } // bản dịch tiếng Việt
    }

    public class ReviewAnswerOption
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        public bool IsCorrect { get; set; }     // ✅ Gửi khi review (đã nộp)
        public bool IsSelected { get; set; }    // user đã chọn cái này không
    }

    // End : get exam preview query


    // Dto exam in process 
    public class InProgressAttemptDto
    {
        public Guid AttemptId { get; set; }
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; }
        public double Progress { get; set; } // phần trăm hoàn thành
        public DateTime StartedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
