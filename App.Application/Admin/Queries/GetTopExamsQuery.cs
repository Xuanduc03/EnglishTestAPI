using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Admin.Queries
{
    // ── 7. Top exams ─────────────────────────────────────────
    public record GetTopExamsQuery(DateRangeFilter? Filter = null, int Top = 10)
        : IRequest<List<TopExamDto>>;

    public class GetTopExamsQueryHandler : IRequestHandler<GetTopExamsQuery, List<TopExamDto>>
    {
        private readonly IAppDbContext _context;
        public GetTopExamsQueryHandler(IAppDbContext context) => _context = context;

        public async Task<List<TopExamDto>> Handle(GetTopExamsQuery request, CancellationToken ct)
        {
            var from = request.Filter?.From ?? DateTime.UtcNow.AddDays(-90);
            var to = request.Filter?.To ?? DateTime.UtcNow;

            return await _context.ExamAttempts
                .Where(a => a.StartedAt >= from && a.StartedAt <= to)
                .GroupBy(a => new { a.ExamId, a.Exam.Title, a.Exam.Code })
                .Select(g => new TopExamDto
                {
                    ExamId = g.Key.ExamId,
                    Title = g.Key.Title,
                    Code = g.Key.Code,
                    AttemptCount = g.Count(),
                    AvgScore = Math.Round(g.Where(a => a.TotalScore != null)
                                              .Average(a => (double?)(a.TotalScore ?? 0)) ?? 0, 1),
                })
                .OrderByDescending(x => x.AttemptCount)
                .Take(request.Top)
                .ToListAsync(ct);
        }
    }

}
