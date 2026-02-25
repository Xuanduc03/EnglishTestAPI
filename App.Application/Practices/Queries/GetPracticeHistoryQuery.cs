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
    // 5. GET PRACTICE HISTORY
    // ============================================

    public record GetPracticeHistoryQuery(
        Guid UserId,
        Guid? CategoryId = null,
        int PageIndex = 1,
        int PageSize = 10
    ) : IRequest<PaginatedResult<PracticeHistoryDto>>;

    public class PracticeHistoryDto
    {
        public Guid SessionId { get; set; }
        public string Title { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double AccuracyPercentage { get; set; }
        public double Score { get; set; }
        public AttemptStatus Status { get; set; }
    }

    public class GetPracticeHistoryQueryHandler : IRequestHandler<GetPracticeHistoryQuery, PaginatedResult<PracticeHistoryDto>>
    {
        private readonly IAppDbContext _context;

        public GetPracticeHistoryQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<PracticeHistoryDto>> Handle(GetPracticeHistoryQuery request, CancellationToken cancellationToken)
        {
            var query = _context.PracticeAttempts
                .Where(a => a.UserId == request.UserId);

            if (request.CategoryId.HasValue)
                query = query.Where(a => a.CategoryId == request.CategoryId);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.StartedAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new PracticeHistoryDto
                {
                    SessionId = a.Id,
                    Title = a.Title,
                    StartedAt = a.StartedAt,
                    SubmittedAt = a.SubmittedAt,
                    TotalQuestions = a.TotalQuestions,
                    CorrectAnswers = a.CorrectAnswers,
                    AccuracyPercentage = a.AccuracyPercentage,
                    Score = a.Score,
                    Status = a.Status
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<PracticeHistoryDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }
    }
}
