using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace App.Application.ExamAttempts.Commands
{
    /// <summary>
    /// Command : Nộp bài + chấm điểm
    /// POST /api/exam-attempts/{attemptId}/submit
    /// </summary>
    public class SubmitExamCommand : IRequest<SubmitExamResult>
    {
        [Required]
        public Guid AttemptId { get; set; }
        public bool IsAutoSubmit { get; set; } = false; // true = hết giờ tự nộp
    }

   

    public class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, SubmitExamResult>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _userService;

        public SubmitExamCommandHandler(IAppDbContext context, ICurrentUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<SubmitExamResult> Handle(
        SubmitExamCommand request,
        CancellationToken cancellationToken)
        {
            // 1. Load attempt
            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken)
                ?? throw new KeyNotFoundException($"Phiên thi {request.AttemptId} ko tìm thấy");

            // 2. Auth check (bỏ qua nếu auto-submit từ server)
            if (!request.IsAutoSubmit)
            {
                var loggedInUserId = _userService.UserId;
                if (loggedInUserId != Guid.Empty && attempt.UserId != loggedInUserId)
                    throw new UnauthorizedAccessException("Cannot submit another user's attempt");
            }

            // 3. Validate có thể nộp
            if (attempt.Status == ExamAttemptStatus.Submitted)
                throw new InvalidOperationException("Attempt already submitted");

            if (attempt.Status == ExamAttemptStatus.Abandoned)
                throw new InvalidOperationException("Attempt was abandoned");

            // 4. Load tất cả ExamAnswers + đáp án đúng trong 1 query
            var examAnswers = await _context.ExamAnswers
                .Where(a => a.ExamAttemptId == request.AttemptId)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.ExamSection)
                      .ThenInclude(s => s.Category)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)   // để biết đáp án đúng
                .ToListAsync(cancellationToken);

            if (!examAnswers.Any())
                throw new InvalidOperationException("No answers found for this attempt");

            // 5. Chấm điểm từng câu
            var now = DateTime.UtcNow;

            foreach (var ea in examAnswers)
            {
                var correctAnswer = ea.ExamQuestions.Question.Answers
                    .FirstOrDefault(a => a.IsCorrect);

                ea.CorrectAnswerId = correctAnswer?.Id;
                ea.IsCorrect = ea.IsAnswered
                            && ea.SelectedAnswerId.HasValue
                            && ea.SelectedAnswerId == correctAnswer?.Id;

                ea.Point = ea.IsCorrect ? (double)ea.ExamQuestions.Point : 0;
                ea.UpdatedAt = now;
            }


            // 6. Tính tổng điểm
            var totalScore = examAnswers.Sum(a => a.Point);
            var maxScore = examAnswers.Sum(a => (double)a.ExamQuestions.Point);
            var correctCount = examAnswers.Count(a => a.IsCorrect);
            var skippedCount = examAnswers.Count(a => !a.IsAnswered);
            var wrongCount = examAnswers.Count - correctCount - skippedCount;

            // 7. Tính Part Summary
            var sectionGroups = examAnswers
            .GroupBy(a => new
            {
                SectionId = a.ExamQuestions.ExamSectionId,
                SectionName = a.ExamQuestions.ExamSection.Category?.Name ?? "Unknown",
                SkillType = a.ExamQuestions.ExamSection.Category?.Name ?? "Unknown",
            })
            .ToList();

            var sectionResults = sectionGroups.Select(g => new ExamSectionResult
            {
                Id = Guid.NewGuid(),
                ExamAttemptId = attempt.Id,
                ExamSectionId = g.Key.SectionId,
                //SkillType = g.Key.SkillType,          
                TotalQuestions = g.Count(),
                CorrectAnswers = g.Count(a => a.IsCorrect),
                ConvertedScore = null,                    
                CreatedAt = now,
                UpdatedAt = now,
            }).ToList();

            // 8. PartSummary cho response (dùng data đã group, không query thêm)
            var partSummaries = sectionGroups.Select(g => new PartSummary
            {
                PartName = g.Key.SectionName,               
                Total = g.Count(),
                Correct = g.Count(a => a.IsCorrect),
                Score = Math.Round(g.Sum(a => a.Point), 2),
            }).ToList();

            // 8. Update attempt
            attempt.Status = ExamAttemptStatus.Submitted;
            attempt.SubmitedAt = now;
            attempt.TotalScore = (int?)totalScore;
            attempt.CorrectAnswers = correctCount;
            attempt.UpdatedAt = now;

            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            // 9. Build result
            return new SubmitExamResult
            {
                AttemptId = attempt.Id,
                SubmittedAt = now,
                TotalScore = Math.Round(totalScore, 2),
                MaxScore = Math.Round(maxScore, 2),
                ScorePercent = maxScore > 0
                    ? Math.Round(totalScore / maxScore * 100, 1)
                    : 0,
                TotalQuestions = examAnswers.Count,
                CorrectAnswers = correctCount,
                WrongAnswers = wrongCount,
                SkippedAnswers = skippedCount,
                DurationSeconds = (int)(now - attempt.StartedAt).TotalSeconds,
                PartSummaries = partSummaries
            };
        }
    }
}
