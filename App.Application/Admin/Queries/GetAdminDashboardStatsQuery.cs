using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Admin.Queries
{
    // ── 1. Dashboard overview ────────────────────────────────
    public record GetAdminDashboardStatsQuery(DateRangeFilter? Filter = null)
        : IRequest<DashboardStatsDto>;

    public class GetAdminDashboardStatsQueryHandler
        : IRequestHandler<GetAdminDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IAppDbContext _context;
        public GetAdminDashboardStatsQueryHandler(IAppDbContext context) => _context = context;

        public async Task<DashboardStatsDto> Handle(
            GetAdminDashboardStatsQuery request, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var filter = request.Filter;

            var from = filter?.From ?? today.AddDays(-30);
            var to = filter?.To ?? now;
            var prevFrom = from.AddDays(-(to - from).TotalDays);

            // Users
            var totalUsers = await _context.Users.CountAsync(ct);
            var newToday = await _context.Users.CountAsync(u => u.CreatedAt >= today, ct);
            var newCurrent = await _context.Users.CountAsync(u => u.CreatedAt >= from && u.CreatedAt <= to, ct);
            var newPrev = await _context.Users.CountAsync(u => u.CreatedAt >= prevFrom && u.CreatedAt < from, ct);

            // Attempts
            var totalAttempts = await _context.ExamAttempts.CountAsync(ct);
            var attemptsToday = await _context.ExamAttempts.CountAsync(a => a.StartedAt >= today, ct);
            var atmCurrent = await _context.ExamAttempts.CountAsync(a => a.StartedAt >= from && a.StartedAt <= to, ct);
            var atmPrev = await _context.ExamAttempts.CountAsync(a => a.StartedAt >= prevFrom && a.StartedAt < from, ct);

            // Score & pass rate (submitted only)
            var submitted = await _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted
                         && a.StartedAt >= from && a.StartedAt <= to)
                .Select(a => new { a.TotalScore })
                .ToListAsync(ct);

            var avgScore = submitted.Any() ? submitted.Average(a => a.TotalScore ?? 0) : 0;

            // Prev period score
            var prevScores = await _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted
                         && a.StartedAt >= prevFrom && a.StartedAt < from)
                .Select(a => (double?)(a.TotalScore ?? 0))
                .ToListAsync(ct);
            var prevAvgScore = prevScores.Any() ? prevScores.Average() ?? 0 : 0;

            // Avg completion time (minutes)
            var completionTimes = await _context.ExamAttempts
                .Where(a => a.Status == ExamAttemptStatus.Submitted
                         && a.SubmitedAt != null
                         && a.StartedAt >= from && a.StartedAt <= to)
                .Select(a => EF.Functions.DateDiffMinute(a.StartedAt, a.SubmitedAt!.Value))
                .ToListAsync(ct);
            var avgCompletionMins = completionTimes.Any() ? completionTimes.Average() : 0;

            // Exams
            var totalExams = await _context.Exams.CountAsync(e => !e.IsDeleted, ct);

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                NewUsersToday = newToday,
                TotalExams = totalExams,
                TotalAttempts = totalAttempts,
                AttemptsToday = attemptsToday,
                AverageScore = Math.Round(avgScore, 1),
                AvgCompletionMins = Math.Round(avgCompletionMins, 1),
                UserGrowthPct = CalcGrowth(newPrev, newCurrent),
                AttemptGrowthPct = CalcGrowth(atmPrev, atmCurrent),
                ScoreGrowthPct = CalcGrowth(prevAvgScore, avgScore),
            };
        }

        private static double CalcGrowth(double prev, double current) =>
            prev == 0 ? 0 : Math.Round((current - prev) / prev * 100, 1);
    }
}
