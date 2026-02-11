using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Practices.Commands
{

    // ============================================
    // 3. SUBMIT PRACTICE SESSION
    // ============================================

    /// <summary>
    /// Submit toàn bộ practice session và tính điểm
    /// </summary>
    public record SubmitPracticeCommand(Guid SessionId) : IRequest<PracticeResultDto>;

    public class SubmitPracticeCommandHandler : IRequestHandler<SubmitPracticeCommand, PracticeResultDto>
    {
        private readonly IAppDbContext _context;

        public SubmitPracticeCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PracticeResultDto> Handle(SubmitPracticeCommand request, CancellationToken cancellationToken)
        {
            // 1. Load attempt với all relationships
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers)
                .Include(a => a.PartResults)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException("Practice attempt not found");

            if (attempt.Status != AttemptStatus.InProgress)
                throw new InvalidOperationException("Practice already submitted");

            // 2. Calculate results
            attempt.Submit();  // Extension method

            // 3. Update part results
            foreach (var partResult in attempt.PartResults)
            {
                var partAnswers = attempt.Answers
                    .Join(
                        _context.Questions.Where(q => q.CategoryId == partResult.CategoryId),
                        a => a.QuestionId,
                        q => q.Id,
                        (a, q) => a
                    )
                    .ToList();

                partResult.CorrectAnswers = partAnswers.Count(a => a.IsCorrect);
                partResult.IncorrectAnswers = partAnswers.Count(a => a.IsAnswered && !a.IsCorrect);
                partResult.UnansweredQuestions = partAnswers.Count(a => !a.IsAnswered);
                partResult.TotalTimeSeconds = partAnswers.Sum(a => a.TimeSpentSeconds);
                partResult.Percentage = partResult.TotalQuestions > 0
                    ? Math.Round((double)partResult.CorrectAnswers / partResult.TotalQuestions * 100, 2)
                    : 0;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Return result DTO
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
                    }
                )
            };
        }
    }
}
