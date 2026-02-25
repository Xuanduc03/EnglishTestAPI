using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Practices.Queries
{
    public record GetInProgressPracticesQuery : IRequest<List<InProgressPracticeDto>>;

    public class GetInProgressPracticesQueryHandler : IRequestHandler<GetInProgressPracticesQuery, List<InProgressPracticeDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetInProgressPracticesQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<InProgressPracticeDto>> Handle(GetInProgressPracticesQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var practices = await _context.PracticeAttempts
               .AsNoTracking()
               .Include(p => p.Category)
               .Include(p => p.Answers)
               .Where(p => p.UserId == userId && p.Status == AttemptStatus.InProgress)
               .OrderByDescending(p => p.UpdatedAt)
               .ToListAsync(cancellationToken);

            return practices.Select(p => new InProgressPracticeDto
            {
                AttemptId = p.Id,
                Title = p.Title,
                CategoryName = p.Category?.Name ?? "Luyện tập",
                Progress = p.TotalQuestions > 0
                    ? Math.Round((double)p.Answers.Count(a => a.IsAnswered) / p.TotalQuestions * 100, 2)
                    : 0,
                StartedAt = p.StartedAt,
                LastUpdated = p.UpdatedAt,
                TimeLimitSeconds = p.TimeLimitSeconds,
                ActualTimeSeconds = p.ActualTimeSeconds
            }).ToList();
        }

    }
}
