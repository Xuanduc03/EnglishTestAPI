using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    public record GetLeaderboardQuery(int Top = 10) : IRequest<List<LeaderboardItemDto>>;

    public class LeaderboardItemDto
    {
        public int Rank { get; set; }
        public string Name { get; set; }
        public int Score { get; set; } // có thể là điểm trung bình
        public int ExamsCompleted { get; set; }
        public int Streak { get; set; }
        public bool IsCurrentUser { get; set; }
    }

    public class LeaderboardDto
    {
        public int Rank { get; set; }
        public Guid UserId { get; set; }
        public string Fullname { get; set; }
        public string? Avatar { get; set; }

    }
}
