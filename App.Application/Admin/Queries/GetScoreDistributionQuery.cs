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
    // ── 4. Score distribution ────────────────────────────────
    public record GetScoreDistributionQuery(DateRangeFilter? Filter = null, Guid? ExamId = null)
        : IRequest<ScoreDistributionDto>;

    public class GetScoreDistributionQueryHandler
        : IRequestHandler<GetScoreDistributionQuery, ScoreDistributionDto>
    {
        private readonly IAppDbContext _context;
        public GetScoreDistributionQueryHandler(IAppDbContext context) => _context = context;

        public async Task<ScoreDistributionDto> Handle(
            GetScoreDistributionQuery request, CancellationToken ct)
        {
            var from = request.Filter?.From ?? DateTime.UtcNow.AddDays(-90);
            var to = request.Filter?.To ?? DateTime.UtcNow;

            var query = _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted
                         && a.TotalScore != null
                         && a.StartedAt >= from && a.StartedAt <= to);

            if (request.ExamId.HasValue)
                query = query.Where(a => a.ExamId == request.ExamId.Value);

            var scores = await query.Select(a => (double)(a.TotalScore ?? 0)).ToListAsync(ct);

            if (!scores.Any())
                return new ScoreDistributionDto();

            // TOEIC 990 scale — 10 buckets × 100
            var buckets = Enumerable.Range(0, 10).Select(i => new ScoreBucketDto
            {
                Min = i * 100,
                Max = (i + 1) * 100,
                Label = $"{i * 100}–{(i + 1) * 100}",
                Count = scores.Count(s => s >= i * 100 && s < (i + 1) * 100),
            }).ToList();
            // edge: score == 990
            buckets.Last().Count += scores.Count(s => s >= 990);

            foreach (var b in buckets)
                b.Pct = Math.Round((double)b.Count / scores.Count * 100, 1);

            var sorted = scores.OrderBy(s => s).ToList();
            var mean = scores.Average();
            var median = sorted.Count % 2 == 0
                ? (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2
                : sorted[sorted.Count / 2];
            var variance = scores.Average(s => Math.Pow(s - mean, 2));

            return new ScoreDistributionDto
            {
                Buckets = buckets,
                Mean = Math.Round(mean, 1),
                Median = Math.Round(median, 1),
                StdDev = Math.Round(Math.Sqrt(variance), 1),
                TotalAttempts = scores.Count,
            };
        }
    }

}
