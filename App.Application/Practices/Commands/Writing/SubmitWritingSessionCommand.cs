using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Practices.Jobs;
using App.Domain.Entities;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Practices.Commands.Writing
{
    // ============================================================
    // SUBMIT WRITING SESSION COMMAND  (nộp toàn bộ bài)
    // ============================================================

    public record SubmitWritingSessionCommand(
        Guid SessionId,
        Guid UserId,
        List<WritingAnswerItem> Answers,
        int TotalTimeSeconds
    ) : IRequest<WritingSessionResultDto>;

    public class SubmitWritingSessionCommandHandler
        : IRequestHandler<SubmitWritingSessionCommand, WritingSessionResultDto>
    {
        private readonly IAppDbContext _context;
        private readonly IBackgroundJobClient _jobs;

        public SubmitWritingSessionCommandHandler(
            IAppDbContext context,
            IBackgroundJobClient jobs)
        {
            _context = context;
            _jobs = jobs;
        }

        public async Task<WritingSessionResultDto> Handle(
            SubmitWritingSessionCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Load attempt + verify ownership
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId
                                       && a.UserId == request.UserId,
                                    cancellationToken)
                ?? throw new InvalidOperationException("Writing session not found.");

            if (attempt.Status == AttemptStatus.Completed)
                throw new InvalidOperationException("Session already submitted.");

            // 2. Lưu tất cả TextAnswer từ request
            var answerMap = attempt.Answers.ToDictionary(a => a.QuestionId);
            var gradingQueue = new List<Guid>();

            foreach (var item in request.Answers)
            {
                if (!answerMap.TryGetValue(item.QuestionId, out var answer)) continue;

                answer.TextAnswer = item.TextAnswer?.Trim();
                answer.TimeSpentSeconds = item.TimeSpentSeconds;

                // Chỉ enqueue AI grading nếu có nội dung
                if (!string.IsNullOrWhiteSpace(answer.TextAnswer))
                {
                    answer.GradingStatus = GradingStatusEnum.NotRequired; // Sẽ update thành Pending ngay dưới
                    gradingQueue.Add(answer.Id);
                }
            }

            // 3. Cập nhật attempt
            attempt.Status = AttemptStatus.Completed;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.TotalTimeSeconds = request.TotalTimeSeconds;

            // Đánh dấu các answer cần grade là Pending
            foreach (var answerId in gradingQueue)
            {
                var ans = attempt.Answers.First(a => a.Id == answerId);
                ans.GradingStatus = GradingStatusEnum.Pending;
            }

            // Các câu bỏ trống → Failed ngay
            foreach (var ans in attempt.Answers.Where(a => string.IsNullOrWhiteSpace(a.TextAnswer)))
                ans.GradingStatus = GradingStatusEnum.Failed;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Enqueue Hangfire jobs cho từng câu có nội dung
            foreach (var answerId in gradingQueue)
            {
                _jobs.Enqueue<PracticeAIGradingJob>(
                    job => job.GradeAnswerAsync(answerId));
            }

            // 5. Cập nhật PracticePartResult time
            await UpdatePartResultTimesAsync(request.SessionId, attempt.Answers.ToList(), cancellationToken);

            // 6. Trả về result (chưa có điểm — đang chờ AI)
            return BuildPendingResult(attempt, attempt.Answers.ToList(), gradingQueue.Count);
        }

        // ── Helpers 

        private async Task UpdatePartResultTimesAsync(
            Guid attemptId,
            List<PracticeAnswer> answers,
            CancellationToken cancellationToken)
        {
            var partResults = await _context.PracticePartResults
                .Where(p => p.PracticeAttemptId == attemptId)
                .ToListAsync(cancellationToken);

            // Load questions để biết CategoryId
            var questionIds = answers.Select(a => a.QuestionId).ToList();
            var questions = await _context.Questions
                .AsNoTracking()
                .Where(q => questionIds.Contains(q.Id))
                .Select(q => new { q.Id, q.CategoryId })
                .ToDictionaryAsync(q => q.Id, q => q.CategoryId, cancellationToken);

            foreach (var partResult in partResults)
            {
                var relatedAnswers = answers
                    .Where(a => questions.TryGetValue(a.QuestionId, out var cat) && cat == partResult.CategoryId)
                    .ToList();

                partResult.TotalTimeSeconds = relatedAnswers.Sum(a => a.TimeSpentSeconds);
                partResult.UnansweredQuestions = relatedAnswers.Count(a => string.IsNullOrWhiteSpace(a.TextAnswer));
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private static WritingSessionResultDto BuildPendingResult(
            PracticeAttempt attempt,
            List<PracticeAnswer> answers,
            int pendingCount)
        {
            return new WritingSessionResultDto
            {
                SessionId = attempt.Id,
                Title = attempt.Title,
                TotalQuestions = attempt.TotalQuestions,
                GradedQuestions = 0,
                IsFullyGraded = pendingCount == 0,
                OverallScore = pendingCount == 0 ? 0 : null,
                TotalTimeSeconds = attempt.TotalTimeSeconds ?? 0,
                // PartResults sẽ được populate đầy đủ bởi GetWritingResultQuery sau khi AI chấm xong
                PartResults = new List<WritingPartResultDto>()
            };
        }
    }
}
