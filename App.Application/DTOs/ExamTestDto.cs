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

        // Điểm TOEIC chuẩn (theo ScoreTable)
        public int ListeningCorrect { get; set; }
        public int ListeningScore { get; set; }   // 5 - 495
        public int ReadingCorrect { get; set; }
        public int ReadingScore { get; set; }     // 5 - 495
        public int TotalScore { get; set; }       // 10 - 990

        // Thống kê chung
        public double MaxScore { get; set; }
        public double ScorePercent { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int SkippedAnswers { get; set; }
        public int DurationSeconds { get; set; }

        public List<PartSummary> PartSummaries { get; set; }
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

        // Thống kê câu trả lời
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int SkippedAnswers { get; set; }
        public double RawScore { get; set; }
        public double MaxScore { get; set; }
        public double ScorePercent { get; set; }

        // Điểm TOEIC
        public bool IsToeic { get; set; }
        public int? ListeningCorrect { get; set; }
        public int? ListeningScore { get; set; }   // 5 - 495
        public int? ReadingCorrect { get; set; }
        public int? ReadingScore { get; set; }     // 5 - 495
        public int? TotalToeicScore { get; set; }  // 10 - 990

        public List<SectionResultDto> SectionResults { get; set; }
    }

    public class SectionResultDto
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }      // "Part 1", "Part 2"...
        public string SkillCode { get; set; }         // "LISTENING" | "READING"
        public string SkillName { get; set; }         // "Phần Nghe" | "Phần đọc hiểu"
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public double Score { get; set; }             // % đúng của Part
        public int? ToeicConvertedScore { get; set; } // Điểm Skill chứa Part này
    }

    // End: GetEXamResultQuery

    /// DTO: GetExamPreviewQuery 
    /// Xem preview lại bài làm 

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


    // ============================================
    // DTOs dùng chung
    // ============================================

    public class ExamAttemptHistoryDto
    {
        public Guid AttemptId { get; set; }
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; }
        public string ExamCode { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? ActualTimeSeconds { get; set; }
        public string Status { get; set; }

        // Scoring
        public int? TotalScore { get; set; }
        public int? ListeningScore { get; set; }
        public int? ReadingScore { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int UnansweredQuestions { get; set; }
        public double AccuracyPercent { get; set; }

        // Section summaries
        public List<ExamSectionSummaryDto> Sections { get; set; } = new();
    }

    public class ExamSectionSummaryDto
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double AccuracyPercent { get; set; }
    }

    public class ExamReviewDto
    {
        public Guid AttemptId { get; set; }
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; }
        public string ExamCode { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int? TotalScore { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public List<ExamReviewSectionDto> Sections { get; set; } = new();
    }

    public class ExamReviewSectionDto
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public int OrderIndex { get; set; }
        public List<ExamReviewQuestionDto> Questions { get; set; } = new();
    }

    public class ExamReviewQuestionDto
    {
        public Guid ExamAnswerId { get; set; }
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public double Point { get; set; }
        public string Content { get; set; }
        public string QuestionType { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Explanation { get; set; }

        // Trắc nghiệm
        public Guid? SelectedAnswerId { get; set; }
        public Guid? CorrectAnswerId { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsAnswered { get; set; }
        public List<ExamReviewAnswerDto> Answers { get; set; } = new();

        // Writing/Speaking
        public string? TextAnswer { get; set; }
        public string? AiFeedback { get; set; }
        public string? AiScoreDetailJson { get; set; }
        public bool IsAiGraded { get; set; }
        public string GradingStatus { get; set; }
    }

    public class ExamReviewAnswerDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public int OrderIndex { get; set; }
    }

}
