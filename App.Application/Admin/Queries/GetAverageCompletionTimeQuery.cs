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

namespace App.Application.Admin.Queries
{
    // ── 6. Average completion time ───────────────────────────
    public record GetAverageCompletionTimeQuery(DateRangeFilter? Filter = null)
        : IRequest<CompletionTimeDto>;

    public class GetAverageCompletionTimeQueryHandler
        : IRequestHandler<GetAverageCompletionTimeQuery, CompletionTimeDto>
    {
        private readonly IAppDbContext _context;
        public GetAverageCompletionTimeQueryHandler(IAppDbContext context) => _context = context;

        public async Task<CompletionTimeDto> Handle(
            GetAverageCompletionTimeQuery request, CancellationToken ct)
        {
            var from = request.Filter?.From ?? DateTime.UtcNow.AddDays(-30);
            var to = request.Filter?.To ?? DateTime.UtcNow;

            var times = await _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted
                         && a.SubmitedAt != null
                         && a.StartedAt >= from && a.StartedAt <= to)
                .Select(a => new
                {
                    Mins = EF.Functions.DateDiffSecond(a.StartedAt, a.SubmitedAt!.Value) / 60.0,
                    Date = a.StartedAt.Date,
                })
                .ToListAsync(ct);

            if (!times.Any())
                return new CompletionTimeDto();

            var sorted = times.Select(t => t.Mins).OrderBy(m => m).ToList();
            var p90idx = (int)Math.Ceiling(sorted.Count * 0.9) - 1;

            return new CompletionTimeDto
            {
                AvgMinutes = Math.Round(sorted.Average(), 1),
                MedianMinutes = Math.Round(sorted[sorted.Count / 2], 1),
                P90Minutes = Math.Round(sorted[Math.Min(p90idx, sorted.Count - 1)], 1),
                ByDay = times
                    .GroupBy(t => t.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new TimeSeriesDataPointDto
                    {
                        Date = g.Key,
                        Label = g.Key.ToString("dd/MM"),
                        Value = Math.Round(g.Average(t => t.Mins), 1),
                    }).ToList(),
            };
        }
    }
}
