using App.Application.DTO;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Students.Queries
{
    /// <summary>
    /// Query :  lấy thông tin dashboard cho user, bao gồm tên, rank, điểm hiện tại, mục tiêu, streak và lịch sử streak 7 ngày
    /// ROLE : User (student)
    /// </summary>
    public record GetDashboardInfoQuery : IRequest<DashboardInfoDto>;

    public class GetDashboardInfoQueryHandler : IRequestHandler<GetDashboardInfoQuery, DashboardInfoDto>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetDashboardInfoQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<DashboardInfoDto> Handle(GetDashboardInfoQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.StudentProfile)
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

            if (user == null)
                throw new UnauthorizedAccessException("Không tìm thấy người dùng.");

            var student = user.StudentProfile;

            // Mục tiêu mặc định (có thể lấy từ bảng cấu hình sau)
            const int defaultTarget = 990;

            // Tính streak history 7 ngày gần nhất
            // Ở đây tạm dùng: nếu streak >= 7 thì cả 7 true, nếu streak = 4 thì 4 true đầu, 3 false cuối
            // Có thể cải thiện sau khi có bảng DailyActivity
            var streakHistory = new List<bool>();
            var streakValue = student?.Streak ?? 0;
            for (int i = 0; i < 7; i++)
            {
                streakHistory.Add(i < streakValue);
            }

            return new DashboardInfoDto
            {
                Name = user.Fullname,
                Rank = student?.MemberLevel ?? "Học viên",
                CurrentScore = student?.Points ?? 0, // Tạm dùng Points, sau này thay bằng điểm thực tế
                TargetScore = defaultTarget,
                Streak = streakValue,
                StreakHistory = streakHistory
            };
        }
    }
}
