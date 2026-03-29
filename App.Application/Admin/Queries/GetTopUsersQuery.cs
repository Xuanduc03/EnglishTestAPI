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

    // ── 8. Top users ─────────────────────────────────────────
    public record GetTopUsersQuery(DateRangeFilter? Filter = null, int Top = 10)
        : IRequest<List<TopUserDto>>;

    public class GetTopUsersQueryHandler : IRequestHandler<GetTopUsersQuery, List<TopUserDto>>
    {
        private readonly IAppDbContext _context;
        public GetTopUsersQueryHandler(IAppDbContext context) => _context = context;

        public async Task<List<TopUserDto>> Handle(GetTopUsersQuery request, CancellationToken ct)
        {
            var from = request.Filter?.From ?? DateTime.UtcNow.AddDays(-90);
            var to = request.Filter?.To ?? DateTime.UtcNow;

            return await _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted
                         && a.StartedAt >= from && a.StartedAt <= to)
                .GroupBy(a => new { a.UserId, a.User.Fullname, a.User.Email })
                .Select(g => new TopUserDto
                {
                    UserId = g.Key.UserId,
                    FullName = g.Key.Fullname,
                    Email = g.Key.Email,
                    AttemptCount = g.Count(),
                    AvgScore = Math.Round(g.Average(a => (double?)(a.TotalScore ?? 0)) ?? 0, 1),
                    BestScore = Math.Round(g.Max(a => (double?)(a.TotalScore ?? 0)) ?? 0, 1),
                })
                .OrderByDescending(x => x.AttemptCount)
                .Take(request.Top)
                .ToListAsync(ct);
        }
    }

}
