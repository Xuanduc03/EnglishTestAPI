using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Leaderboards.Queries
{
    public record GetLeaderboardQuery(int Limit = 10) : IRequest<LeaderboardResult>;

    // LeaderboardEntryDto.cs
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public Guid UserId { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int Points { get; set; }
        public int Streak { get; set; }
        public string? Medal { get; set; }
    }

    // CurrentUserRankDto.cs
    public class CurrentUserRankDto
    {
        public int Rank { get; set; }
        public int Points { get; set; }
        public int Streak { get; set; }
    }

    // LeaderboardResult.cs
    public class LeaderboardResult
    {
        public List<LeaderboardEntryDto> Leaderboard { get; set; } = new();
        public CurrentUserRankDto? CurrentUserRank { get; set; }
    }
    public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, LeaderboardResult>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetLeaderboardQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<LeaderboardResult> Handle(GetLeaderboardQuery request, CancellationToken ct)
        {
            // Lấy top N student theo Points giảm dần
            var topStudents = await _context.Students
                .Include(s => s.User) // nếu cần lấy AvatarUrl, Fullname từ User
                .OrderByDescending(s => s.Points)
                .Take(request.Limit)
                .Select(s => new LeaderboardEntryDto
                {
                    UserId = s.UserId,
                    Fullname = s.User.Fullname, // hoặc s.Fullname nếu Student đã có Fullname
                    AvatarUrl = s.User.AvatarUrl,
                    Points = s.Points,
                    Streak = s.Streak,
                    Rank = 0 // tạm thời
                })
                .ToListAsync(ct);

            // Gán rank và huy chương
            for (int i = 0; i < topStudents.Count; i++)
            {
                topStudents[i].Rank = i + 1;
                if (i == 0) topStudents[i].Medal = "🥇";
                else if (i == 1) topStudents[i].Medal = "🥈";
                else if (i == 2) topStudents[i].Medal = "🥉";
            }

            // Tìm thứ hạng của current user
            CurrentUserRankDto? currentRank = null;
            var currentUserId = _currentUser.UserId; // giả sử ICurrentUserService có UserId property
            if (currentUserId.HasValue)
            {
                // Lấy tất cả student sắp xếp theo điểm (không limit) để tìm rank
                var allOrdered = await _context.Students
                    .OrderByDescending(s => s.Points)
                    .Select(s => new { s.UserId, s.Points, s.Streak })
                    .ToListAsync(ct);

                var rank = allOrdered.FindIndex(s => s.UserId == currentUserId.Value) + 1;
                if (rank > 0)
                {
                    var userData = allOrdered[rank - 1];
                    currentRank = new CurrentUserRankDto
                    {
                        Rank = rank,
                        Points = userData.Points,
                        Streak = userData.Streak
                    };
                }
            }

            return new LeaderboardResult
            {
                Leaderboard = topStudents,
                CurrentUserRank = currentRank
            };
        }
    }
}
