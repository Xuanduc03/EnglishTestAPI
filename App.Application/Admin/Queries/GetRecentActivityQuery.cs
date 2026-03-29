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
    // ── 10. Recent activity ──────────────────────────────────
    public record GetRecentActivityQuery(int Take = 20) : IRequest<List<RecentActivityDto>>;

    public class GetRecentActivityQueryHandler
        : IRequestHandler<GetRecentActivityQuery, List<RecentActivityDto>>
    {
        private readonly IAppDbContext _context;
        public GetRecentActivityQueryHandler(IAppDbContext context) => _context = context;

        public async Task<List<RecentActivityDto>> Handle(
            GetRecentActivityQuery request, CancellationToken ct)
        {
            // Submitted attempts
            var submissions = await _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted && a.SubmitedAt != null)
                .OrderByDescending(a => a.SubmitedAt)
                .Take(request.Take)
                .Select(a => new RecentActivityDto
                {
                    UserId = a.UserId,
                    UserName = a.User.Fullname,
                    Action = "submitted",
                    ExamTitle = a.Exam.Title,
                    Score = (double?)(a.TotalScore ?? 0),
                    OccuredAt = a.SubmitedAt!.Value,
                })
                .ToListAsync(ct);

            // New registrations
            var registrations = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(request.Take)
                .Select(u => new RecentActivityDto
                {
                    UserId = u.Id,
                    UserName = u.Fullname,
                    Action = "registered",
                    ExamTitle = string.Empty,
                    Score = null,
                    OccuredAt = u.CreatedAt,
                })
                .ToListAsync(ct);

            return submissions
                .Concat(registrations)
                .OrderByDescending(a => a.OccuredAt)
                .Take(request.Take)
                .ToList();
        }
    }
}
