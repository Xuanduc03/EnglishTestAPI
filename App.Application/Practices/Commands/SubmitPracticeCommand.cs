using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace App.Application.Practices.Commands
{
    // ============================================
    // SUBMIT PRACTICE SESSION (gộp chung 1 lần)
    // ============================================

    /// <summary>
    /// Submit toàn bộ practice session:
    /// - Nhận list answers từ FE (1 lần duy nhất)
    /// - Chấm đúng/sai cho từng câu
    /// - Tính điểm tổng + per-part
    /// </summary>
    public record SubmitPracticeCommand(
        Guid sessionId, List<SubmitAnswerItem> Answers, int totalTimeSeconds) : IRequest<PracticeResultDto>;

    /// <summary>Câu trả lời của 1 question — hỗ trợ cả 3 loại</summary>
    public record SubmitAnswerItem
    (   Guid QuestionId,
        Guid? AnswerId,
        bool IsMarkedForReview,
        string? TextAnswer = null,
        int? WordCount = null,
        string? AudioUrl = null,
        string? AudioPublicId = null,
        int? RecordingDurationSeconds = null
    );

    // handler 
    public class SubmitPraticeCommandHandler(
        IAppDbContext context,
        IBackgroundJobClient jobs) : IRequestHandler<SubmitPracticeCommand, PracticeResultDto>
    {
        private readonly IAppDbContext _context = context;
        private readonly IBackgroundJobClient _jobs = jobs;

        public async Task<PracticeResultDto> Handle(SubmitPracticeCommand request, CancellationToken cancellation)
        {
            // 1. load attempt 
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers)
                    .ThenInclude(qa => qa.Question)
                      .ThenInclude(q => q.Answers)
                .Include(a => a.PartResults)
                .FirstOrDefaultAsync(a => a.Id == request.sessionId, cancellation)
                ?? throw new KeyNotFoundException("Phiên luyện tập ko tồn tại");

            if (attempt.Status != AttemptStatus.InProgress)
                throw new InvalidOperationException("Pratice đã được nạp");

            // 2. build look up
            var answerLookup = request.Answers.ToDictionary(a => a.QuestionId);

            // 3. chấm + lưu từng câu 
            var aiAnswerIds = new List<Guid>(); // collect để enqueue job sau

            foreach (var practiceAnswer in attempt.Answers)
            {
                if (!answerLookup.TryGetValue(practiceAnswer.QuestionId, out var submitted))
                {
                    // bỏ trống 
                    practiceAnswer.SelectedAnswerId = null;
                    practiceAnswer.IsCorrect = false;
                    continue;
                }

                var qType = practiceAnswer.Question.QuestionType;

                // trac nghiem
                if (qType == QuestionTypeEnum.SingleChoice || qType == QuestionTypeEnum.MultipleChoice)
                {
                    var correctAnswer = practiceAnswer.Question.Answers
                        .FirstOrDefault(a => a.IsCorrect);

                    practiceAnswer.SelectedAnswerId = submitted.AnswerId;
                    practiceAnswer.IsCorrect = submitted.AnswerId.HasValue && submitted.AnswerId == correctAnswer?.Id;
                    practiceAnswer.IsMarkedForReview = submitted.IsMarkedForReview;
                    practiceAnswer.CreatedAt = DateTime.UtcNow;
                    practiceAnswer.GradingStatus = GradingStatusEnum.NotRequired;
                }
            }

            // 4. cập nhật attempt
            attempt.ActualTimeSeconds = request.totalTimeSeconds;
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.Status = attempt.IsTimedOut ? AttemptStatus.TimedOut : AttemptStatus.Submitted;

            // Chỉ tính trắc nghiệm trước, ai answers sẽ cập nhật lại sau khi chấm
            var gradedAnswer = attempt.Answers.Where(a => a.GradingStatus == GradingStatusEnum.NotRequired).ToList();

            attempt.CorrectAnswers = gradedAnswer.Count(a => a.IsCorrect); // số câu đúng
            attempt.IncorrectAnswers = gradedAnswer.Count(a => a.IsAnswered && !a.IsCorrect); // số câu sai
            attempt.UnansweredQuestions = attempt.Answers.Count(a => !a.IsAnswered); // số câu ko trả lời
            // phần trăm câu trả lời đúng
            attempt.AccuracyPercentage = attempt.TotalQuestions > 0
                ? Math.Round((double)attempt.CorrectAnswers / attempt.TotalQuestions * 100, 2)
                : 0;
            // điểm tạm thời 
            attempt.Score = attempt.TotalQuestions > 0 ? Math.Round((double)attempt.CorrectAnswers / attempt.TotalQuestions * 100, 2) : 0;

            // ── 5. Per-part results
            foreach (var partResult in attempt.PartResults)
            {
                // lấy câu hỏi theo part
                var partQuestionIds = await _context.Questions.Where(q => q.CategoryId == partResult.CategoryId).Select(q => q.Id).ToListAsync(cancellation);

                // lấy câu trả lời theo câu hỏi
                var partAnswers = attempt.Answers.Where(a => partQuestionIds.Contains(a.QuestionId)).ToList();

                partResult.CorrectAnswers = partAnswers.Count(a => a.IsCorrect);
                partResult.IncorrectAnswers = partAnswers.Count(a => a.IsAnswered && !a.IsCorrect);
                partResult.UnansweredQuestions = partAnswers.Count(a => !a.IsAnswered);
                partResult.TotalQuestions = request.totalTimeSeconds;
                partResult.Percentage = partResult.TotalQuestions > 0 ? Math.Round((double)partResult.CorrectAnswers / partResult.TotalQuestions * 100, 2) : 0;
            }

            // ── 6. Streak + Points
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == attempt.UserId, cancellation);

            if (student != null)
            {
                var today = DateTime.UtcNow.Date;
                if (student.LastStreakDate == today.AddDays(-1))
                    student.Streak += 1;
                else if (student.LastStreakDate != today)
                    student.Streak = 1;
                student.LastStreakDate = today;
                student.Points += 5;

                var stats = await _context.UserStatistics
                    .FirstOrDefaultAsync(us => us.UserId == attempt.UserId, cancellation);

                if (stats == null)
                {
                    _context.UserStatistics.Add(new UserStatistics
                    {
                        UserId = attempt.UserId,
                        TotalExamsCompleted = 1,
                        AverageScore = (decimal)attempt.Score,
                        CurrentStreak = student.Streak,
                        LastActivityDate = today
                    });
                }
                else
                {
                    stats.TotalExamsCompleted += 1;
                    stats.AverageScore = (stats.AverageScore * (stats.TotalExamsCompleted - 1)
                        + (decimal)attempt.Score) / stats.TotalExamsCompleted;
                    stats.CurrentStreak = student.Streak;
                    stats.LastActivityDate = today;
                }
            }

            // 7. save db
            using var transaction = await _context.BeginTransactionAsync(cancellation);
            try
            {
                await _context.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);
            }
            catch
            {
                await transaction.RollbackAsync(cancellation);
                throw;
            }

            // 9 -return 
            var pendingAiCount = aiAnswerIds.Count;

            return new PracticeResultDto
            {
                SessionId = attempt.Id,
                TotalQuestions = attempt.TotalQuestions,
                CorrectAnswers = attempt.CorrectAnswers,
                IncorrectAnswers = attempt.IncorrectAnswers,
                UnansweredQuestions = attempt.UnansweredQuestions,
                Score = attempt.Score,
                AccuracyPercentage = attempt.AccuracyPercentage,
                TotalTime = TimeSpan.FromSeconds(attempt.ActualTimeSeconds ?? 0),
                PendingAiGrading = pendingAiCount,           // FE biết còn chờ AI
                IsFullyGraded = pendingAiCount == 0,
                PartResults = attempt.PartResults.ToDictionary(
                    pr => pr.PartName,
                    pr => new PartResultDto
                    {
                        PartName = pr.PartName,
                        PartNumber = pr.PartNumber,
                        Total = pr.TotalQuestions,
                        Correct = pr.CorrectAnswers,
                        Incorrect = pr.IncorrectAnswers,
                        Unanswered = pr.UnansweredQuestions,
                        Percentage = pr.Percentage,
                        AverageTimePerQuestion = pr.AverageTimePerQuestion
                    })
            };
        }

        private static int CountWords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            return text.Trim().Split(
                new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }

}