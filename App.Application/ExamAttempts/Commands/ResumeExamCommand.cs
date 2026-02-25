using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.ExamAttempts.Commands
{
    // ============================================
    // REQUEST & RESPONSE
    // ============================================

    public record ResumeExamCommand(Guid AttemptId) : IRequest<ResumeExamResult>;

    public class ResumeExamResult
    {
        // Metadata
        public Guid AttemptId { get; set; }
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int LastAnsweredIndex { get; set; }

        // Thời gian
        public int? TimeRemainingSeconds { get; set; } // null = không giới hạn
        public DateTime? ExpiresAt { get; set; }
        public bool IsTimedOut { get; set; }

        // Answers đã chọn (để FE restore state)
        public List<ResumeAnswerItem> Answers { get; set; } = new();
    }

    public class ResumeAnswerItem
    {
        public Guid QuestionId { get; set; }
        public Guid? SelectedAnswerId { get; set; }
        public bool IsMarkedForReview { get; set; }
        public bool IsAnswered { get; set; }
    }

    // ============================================
    // HANDLER
    // ============================================

    public class ResumeExamCommandHandler : IRequestHandler<ResumeExamCommand, ResumeExamResult>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _userService;
        public ResumeExamCommandHandler(IAppDbContext context, ICurrentUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<ResumeExamResult> Handle(ResumeExamCommand request, CancellationToken cancellationToken)
        {
            // ── 1. Load ExamAttempt ───────────────────────────────────────
            var currentUser = _userService.UserId;
            var attempt = await _context.ExamAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(
                    a => a.Id == request.AttemptId && a.UserId == currentUser,
                    cancellationToken
                );

            if (attempt == null)
                throw new KeyNotFoundException($"Không tìm thấy lượt thi {request.AttemptId}");

            // ── 2. Kiểm tra trạng thái ────────────────────────────────────
            if (attempt.Status == ExamAttemptStatus.Submitted)
                throw new InvalidOperationException("Bài thi này đã nộp, không thể tiếp tục.");

            if (attempt.Status == ExamAttemptStatus.Abandoned)
                throw new InvalidOperationException("Bài thi này đã bị hủy.");

            // ── 3. Kiểm tra hết giờ → auto submit ────────────────────────
            var now = DateTime.UtcNow;
            var isTimedOut = attempt.ExpiresAt.HasValue && attempt.ExpiresAt.Value < now;

            if (isTimedOut)
            {
                // Auto submit khi hết giờ
                attempt.Status = ExamAttemptStatus.TimedOut;
                attempt.SubmitedAt = now;
                attempt.ActualTimeSeconds = attempt.TimeLimitSeconds; // đã dùng hết giờ

                // Chấm điểm các câu đã trả lời
                await GradeAnswersAsync(attempt, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

                // Trả về result với IsTimedOut = true để FE redirect sang trang kết quả
                return new ResumeExamResult
                {
                    AttemptId = attempt.Id,
                    ExamId = attempt.ExamId,
                    TotalQuestions = attempt.TotalQuestions,
                    LastAnsweredIndex = attempt.LastAnsweredIndex ?? 0,
                    IsTimedOut = true,
                    TimeRemainingSeconds = 0,
                    ExpiresAt = attempt.ExpiresAt,
                    Answers = MapAnswers(attempt.Answers)
                };
            }

            // ── 4. Tính thời gian còn lại ─────────────────────────────────
            int? timeRemainingSeconds = null;
            if (attempt.ExpiresAt.HasValue)
            {
                timeRemainingSeconds = (int)(attempt.ExpiresAt.Value - now).TotalSeconds;
            }

            // ── 5. Load ExamTitle ─────────────────────────────────────────
            var examTitle = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Id == attempt.ExamId)
                .Select(e => e.Title)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            // ── 6. Trả về result ──────────────────────────────────────────
            return new ResumeExamResult
            {
                AttemptId = attempt.Id,
                ExamId = attempt.ExamId,
                ExamTitle = examTitle,
                TotalQuestions = attempt.TotalQuestions,
                LastAnsweredIndex = attempt.LastAnsweredIndex ?? 0,
                IsTimedOut = false,
                TimeRemainingSeconds = timeRemainingSeconds,
                ExpiresAt = attempt.ExpiresAt,
                Answers = MapAnswers(attempt.Answers)
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>Chấm điểm các câu đã trả lời khi auto-submit do hết giờ</summary>
        private async Task GradeAnswersAsync(ExamAttempt attempt, CancellationToken cancellationToken)
        {
            var questionIds = attempt.Answers
                .Where(a => a.SelectedAnswerId.HasValue)
                .Select(a => a.QuestionId)
                .ToList();

            if (!questionIds.Any()) return;

            // Load correct answers 1 lần
            var correctAnswerMap = await _context.Answers
                .AsNoTracking()
                .Where(a => questionIds.Contains(a.QuestionId) && a.IsCorrect)
                .ToDictionaryAsync(a => a.QuestionId, a => a.Id, cancellationToken);

            foreach (var examAnswer in attempt.Answers)
            {
                if (!examAnswer.SelectedAnswerId.HasValue) continue;

                examAnswer.IsCorrect = correctAnswerMap.TryGetValue(examAnswer.QuestionId, out var correctId)
                    && examAnswer.SelectedAnswerId.Value == correctId;
            }

            attempt.CorrectAnswers = attempt.Answers.Count(a => a.IsCorrect);
            attempt.IncorrectAnswers = attempt.Answers.Count(a => a.IsAnswered && !a.IsCorrect);
            attempt.UnanswerQuestions = attempt.Answers.Count(a => !a.IsAnswered);
        }

        private static List<ResumeAnswerItem> MapAnswers(ICollection<ExamAnswer> answers) =>
            answers.Select(a => new ResumeAnswerItem
            {
                QuestionId = a.QuestionId,
                SelectedAnswerId = a.SelectedAnswerId,
                IsMarkedForReview = false, // ExamAnswer chưa có field này, thêm nếu cần
                IsAnswered = a.IsAnswered,
            }).ToList();
    }
}