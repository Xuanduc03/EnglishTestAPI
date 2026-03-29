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
    // ── 5. Pass rate by exam ─────────────────────────────────
    public record GetPassRateByExamQuery(DateRangeFilter? Filter = null, int Top = 10)
        : IRequest<List<PassRateDto>>;

    public class GetPassRateByExamQueryHandler
        : IRequestHandler<GetPassRateByExamQuery, List<PassRateDto>>
    {
        private readonly IAppDbContext _context;
        public GetPassRateByExamQueryHandler(IAppDbContext context) => _context = context;

        public async Task<List<PassRateDto>> Handle(
            GetPassRateByExamQuery request, CancellationToken ct)
        {
            var from = request.Filter?.From ?? DateTime.UtcNow.AddDays(-90);
            var to = request.Filter?.To ?? DateTime.UtcNow;

            var data = await _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted
                         && a.StartedAt >= from && a.StartedAt <= to)
                .GroupBy(a => new { a.ExamId, a.Exam.Title })
                .Select(g => new
                {
                    g.Key.ExamId,
                    g.Key.Title,
                    Total = g.Count(),
                    Avg = g.Average(a => (double?)(a.TotalScore ?? 0)) ?? 0,
                })
                .OrderByDescending(x => x.Total)
                .Take(request.Top)
                .ToListAsync(ct);

            return data.Select(d => new PassRateDto
            {
                ExamId = d.ExamId,
                ExamTitle = d.Title,
                Total = d.Total,
                AvgScore = Math.Round(d.Avg, 1),
            }).ToList();
        }
    }
}
