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
  // ── 3. Exam submission stats ─────────────────────────────
    public record GetExamSubmissionStatsQuery(DateRangeFilter Filter) : IRequest<ExamSubmissionStatsDto>;

    public class GetExamSubmissionStatsQueryHandler
        : IRequestHandler<GetExamSubmissionStatsQuery, ExamSubmissionStatsDto>
    {
        private readonly IAppDbContext _context;
        public GetExamSubmissionStatsQueryHandler(IAppDbContext context) => _context = context;

        public async Task<ExamSubmissionStatsDto> Handle(
            GetExamSubmissionStatsQuery request, CancellationToken ct)
        {
            var from = request.Filter.From ?? DateTime.UtcNow.AddDays(-30);
            var to = request.Filter.To ?? DateTime.UtcNow;

            var attempts = await _context.ExamAttempts
                .Where(a => a.StartedAt >= from && a.StartedAt <= to)
                .Select(a => new { a.StartedAt, a.Status, a.TotalScore })
                .ToListAsync(ct);

            var submitted = attempts.Where(a => a.Status == ExamAttemptStatus.Submitted).ToList();

            var gran = request.Filter.Granularity;
            var dates = Enumerable.Range(0, (int)(to - from).TotalDays + 1)
                .Select(i => from.AddDays(i).Date).ToList();

            return new ExamSubmissionStatsDto
            {
                TotalSubmitted = submitted.Count,
                AvgScore = submitted.Any() ? Math.Round(submitted.Average(a => a.TotalScore ?? 0), 1) : 0,
                Submissions = dates.Select(d => new TimeSeriesDataPointDto
                {
                    Date = d,
                    Label = d.ToString("dd/MM"),
                    Value = attempts.Count(a => a.StartedAt.Date == d),
                }).ToList(),
                Completions = dates.Select(d => new TimeSeriesDataPointDto
                {
                    Date = d,
                    Label = d.ToString("dd/MM"),
                    Value = submitted.Count(a => a.StartedAt.Date == d),
                }).ToList(),
            };
        }
    }
}
