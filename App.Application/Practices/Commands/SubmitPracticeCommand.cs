using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        Guid SessionId,
        List<SubmitAnswerItem> Answers,
        int TotalTimeSeconds
    ) : IRequest<PracticeResultDto>;

    /// <summary>Câu trả lời của 1 question</summary>
    public record SubmitAnswerItem(
        Guid QuestionId,
        Guid? AnswerId,
        bool IsMarkedForReview
    );

    public class SubmitPracticeCommandHandler : IRequestHandler<SubmitPracticeCommand, PracticeResultDto>
    {
        private readonly IAppDbContext _context;

        public SubmitPracticeCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PracticeResultDto> Handle(SubmitPracticeCommand request, CancellationToken cancellationToken)
        {
            // ── 1. Load attempt ───────────────────────────────
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers)
                    .ThenInclude(pa => pa.Question)
                        .ThenInclude(q => q.Answers)
                .Include(a => a.PartResults)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException("Practice attempt not found");

            if (attempt.Status != AttemptStatus.InProgress)
                throw new InvalidOperationException("Practice already submitted");

            // ── 2. Build lookup: questionId → answer item từ FE ──
            var answerLookup = request.Answers
                .ToDictionary(a => a.QuestionId);

            // ── 3. Chấm điểm từng PracticeAnswer ─────────────
            foreach (var practiceAnswer in attempt.Answers)
            {
                var correctAnswer = practiceAnswer.Question.Answers
                    .FirstOrDefault(a => a.IsCorrect);

                if (answerLookup.TryGetValue(practiceAnswer.QuestionId, out var submitted))
                {
                    practiceAnswer.SelectedAnswerId = submitted.AnswerId;
                    practiceAnswer.IsCorrect = submitted.AnswerId.HasValue
                        && submitted.AnswerId == correctAnswer?.Id;
                    practiceAnswer.IsMarkedForReview = submitted.IsMarkedForReview;
                    practiceAnswer.AnsweredAt = DateTime.UtcNow;
                }
                else
                {
                    // Câu không có trong payload → bỏ trống
                    practiceAnswer.SelectedAnswerId = null;
                    practiceAnswer.IsCorrect = false;
                }
            }

            // ── 4. Tính điểm tổng + đánh dấu thời gian ───────
            attempt.ActualTimeSeconds = request.TotalTimeSeconds;
            attempt.Submit(); // domain method: tính CorrectAnswers, Score, set Status = Completed

            // ── 5. Cập nhật per-part results ──────────────────
            foreach (var partResult in attempt.PartResults)
            {
                // Lấy câu hỏi thuộc part này
                var partQuestionIds = await _context.Questions
                    .Where(q => q.CategoryId == partResult.CategoryId)
                    .Select(q => q.Id)
                    .ToListAsync(cancellationToken);

                var partAnswers = attempt.Answers
                    .Where(a => partQuestionIds.Contains(a.QuestionId))
                    .ToList();

                partResult.CorrectAnswers      = partAnswers.Count(a => a.IsCorrect);
                partResult.IncorrectAnswers    = partAnswers.Count(a => a.IsAnswered && !a.IsCorrect);
                partResult.UnansweredQuestions = partAnswers.Count(a => !a.IsAnswered);
                partResult.TotalTimeSeconds    = request.TotalTimeSeconds; // chia đều hoặc để tổng tuỳ yêu cầu
                partResult.Percentage = partResult.TotalQuestions > 0
                    ? Math.Round((double)partResult.CorrectAnswers / partResult.TotalQuestions * 100, 2)
                    : 0;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // ── 6. Trả về kết quả ─────────────────────────────
            return new PracticeResultDto
            {
                SessionId            = attempt.Id,
                TotalQuestions       = attempt.TotalQuestions,
                CorrectAnswers       = attempt.CorrectAnswers,
                IncorrectAnswers     = attempt.IncorrectAnswers,
                UnansweredQuestions  = attempt.UnansweredQuestions,
                Score                = attempt.Score,
                AccuracyPercentage   = attempt.AccuracyPercentage,
                TotalTime            = TimeSpan.FromSeconds(attempt.ActualTimeSeconds ?? 0),
                PartResults          = attempt.PartResults.ToDictionary(
                    pr => pr.PartName,
                    pr => new PartResultDto
                    {
                        PartName                = pr.PartName,
                        PartNumber              = pr.PartNumber,
                        Total                   = pr.TotalQuestions,
                        Correct                 = pr.CorrectAnswers,
                        Incorrect               = pr.IncorrectAnswers,
                        Unanswered              = pr.UnansweredQuestions,
                        Percentage              = pr.Percentage,
                        AverageTimePerQuestion  = pr.AverageTimePerQuestion
                    }
                )
            };
        }
    }
}