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
    // ── 2. User growth ───────────────────────────────────────
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Filter"></param>
    public record GetUserGrowthStatsQuery(DateRangeFilter Filter) : IRequest<UserGrowthDto>;

    public class GetUserGrowthStatsQueryHandler
        : IRequestHandler<GetUserGrowthStatsQuery, UserGrowthDto>
    {
        private readonly IAppDbContext _context;
        public GetUserGrowthStatsQueryHandler(IAppDbContext context) => _context = context;

        public async Task<UserGrowthDto> Handle(
            GetUserGrowthStatsQuery request, CancellationToken ct)
        {
            var from = request.Filter.From ?? DateTime.UtcNow.AddDays(-30);
            var to = request.Filter.To ?? DateTime.UtcNow;

            var users = await _context.Users
                .Where(u => u.CreatedAt >= from && u.CreatedAt <= to)
                .Select(u => u.CreatedAt)
                .ToListAsync(ct);

            var points = GroupByGranularity(users, request.Filter.Granularity, from, to);

            // Cumulative total
            var totalBefore = await _context.Users.CountAsync(u => u.CreatedAt < from, ct);
            double running = totalBefore;
            var totalSeries = points.Select(p =>
            {
                running += p.Value;
                return new TimeSeriesDataPointDto { Date = p.Date, Label = p.Label, Value = running };
            }).ToList();

            return new UserGrowthDto
            {
                NewUsers = points,
                TotalUsers = totalSeries,
                TotalNewUsers = users.Count,
            };
        }

        private static List<TimeSeriesDataPointDto> GroupByGranularity(
            List<DateTime> dates, Granularity gran, DateTime from, DateTime to)
        {
            return gran switch
            {
                Granularity.Month => Enumerable
                    .Range(0, (int)((to - from).TotalDays / 30) + 1)
                    .Select(i => from.AddMonths(i))
                    .Where(d => d <= to)
                    .Select(d => new TimeSeriesDataPointDto
                    {
                        Date = d,
                        Label = d.ToString("MM/yyyy"),
                        Value = dates.Count(x => x.Year == d.Year && x.Month == d.Month),
                    }).ToList(),

                Granularity.Week => Enumerable
                    .Range(0, (int)((to - from).TotalDays / 7) + 1)
                    .Select(i => from.AddDays(i * 7))
                    .Where(d => d <= to)
                    .Select(d => new TimeSeriesDataPointDto
                    {
                        Date = d,
                        Label = $"T{d:dd/MM}",
                        Value = dates.Count(x => x >= d && x < d.AddDays(7)),
                    }).ToList(),

                _ => Enumerable
                    .Range(0, (int)(to - from).TotalDays + 1)
                    .Select(i => from.AddDays(i))
                    .Select(d => new TimeSeriesDataPointDto
                    {
                        Date = d,
                        Label = d.ToString("dd/MM"),
                        Value = dates.Count(x => x.Date == d.Date),
                    }).ToList(),
            };
        }
    }

}
