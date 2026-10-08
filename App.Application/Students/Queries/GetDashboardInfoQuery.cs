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
            if (student == null || !student.IsActive)
                throw new KeyNotFoundException("Hồ sơ học viên không tồn tại hoặc không hoạt động");

            // Mục tiêu mặc định (có thể lấy từ bảng cấu hình sau)
            const int defaultTarget = 990;

            // Các ngày trong chuỗi học liên tiếp, theo thứ tự cũ đến mới.
            var streakHistory = new List<bool>();
            var streakValue = student.Streak;
            var lastStreakDate = student.LastStreakDate?.Date;
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.UtcNow.Date.AddDays(-i);
                streakHistory.Add(lastStreakDate.HasValue && streakValue > 0
                    && day <= lastStreakDate.Value
                    && day > lastStreakDate.Value.AddDays(-streakValue));
            }

            return new DashboardInfoDto
            {
                Name = user.FullName,
                Rank = student.MemberLevel.ToString(),
                CurrentScore = student.Points, // Tạm dùng Points, sau này thay bằng điểm thực tế
                TargetScore = defaultTarget,
                Streak = streakValue,
                StreakHistory = streakHistory
            };
        }
    }
}
