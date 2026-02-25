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

namespace App.Application.Practices.Queries
{
    // ============================================
    // 4. GET PRACTICE RESULT
    // ============================================

    public record GetPracticeResultQuery(Guid SessionId) : IRequest<PracticeResultDto>;

    public class GetPracticeResultQueryHandler : IRequestHandler<GetPracticeResultQuery, PracticeResultDto>
    {
        private readonly IAppDbContext _context;

        public GetPracticeResultQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PracticeResultDto> Handle(GetPracticeResultQuery request, CancellationToken cancellationToken)
        {
            var attempt = await _context.PracticeAttempts
                .AsNoTracking()
                .Include(a => a.PartResults)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException("Practice result not found");

            if (attempt.Status == AttemptStatus.InProgress)
                throw new InvalidOperationException("Practice not submitted yet");

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
