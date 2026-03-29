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
    // ── 9. Skill stats ───────────────────────────────────────
    public record GetSkillStatsQuery(DateRangeFilter? Filter = null) : IRequest<List<SkillStatsDto>>;

    public class GetSkillStatsQueryHandler : IRequestHandler<GetSkillStatsQuery, List<SkillStatsDto>>
    {
        private readonly IAppDbContext _context;
        public GetSkillStatsQueryHandler(IAppDbContext context) => _context = context;

        public async Task<List<SkillStatsDto>> Handle(GetSkillStatsQuery request, CancellationToken ct)
        {
            var from = request.Filter?.From ?? DateTime.UtcNow.AddDays(-90);
            var to = request.Filter?.To ?? DateTime.UtcNow;

            var answers = await _context.ExamAnswers
                .Where(a => a.Attempt.Status == ExamAttemptStatus.Submitted
                         && a.Attempt.StartedAt >= from
                         && a.Attempt.StartedAt <= to
                         && a.ExamQuestions.ExamSection.Category != null)
                .GroupBy(a => new
                {
                    Code = a.ExamQuestions.ExamSection.Category!.Code,
                    Name = a.ExamQuestions.ExamSection.Category!.Name,
                })
                .Select(g => new SkillStatsDto
                {
                    PartName = g.Key.Code,
                    SkillType = g.Key.Name,
                    TotalAnswered = g.Count(),
                    TotalQuestions = g.Count(),
                    AvgAccuracy = Math.Round(g.Count(a => a.IsCorrect) / (double)g.Count() * 100, 1),
                    AvgScore = Math.Round(g.Sum(a => (double)a.Point), 1),
                })
                .ToListAsync(ct);

            return answers.OrderBy(s => s.PartName).ToList();
        }
    }
}
