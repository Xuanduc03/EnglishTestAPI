using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Leaderboards.Commands
{
    public record UpdatePointsAndStreakCommand(Guid UserId, int PointsEarned, DateTime ActivityDate) : IRequest;

    public class UpdatePointsAndStreakCommandHandler : IRequestHandler<UpdatePointsAndStreakCommand>
    {
        private readonly IAppDbContext _context;
        public UpdatePointsAndStreakCommandHandler(IAppDbContext context)
        {
            _context = context;
        }
        public async Task Handle(UpdatePointsAndStreakCommand request, CancellationToken cancellationToken)
        {
            var student = await _context.Students
             .FirstOrDefaultAsync(s => s.UserId == request.UserId, cancellationToken);

            if (student == null) return;

            // Cập nhật streak
            var today = request.ActivityDate.Date;
            var lastActivity = student.LastStreakDate?.Date;

            if (lastActivity == null)
            {
                student.Streak = 1;
            }
            else if (lastActivity == today)
            {
                // Đã hoạt động hôm nay, giữ nguyên
            }
            else if (lastActivity == today.AddDays(-1))
            {
                student.Streak++;
            }
            else
            {
                student.Streak = 1;
            }

            student.LastStreakDate = today;

            // Cập nhật điểm
            student.Points += request.PointsEarned;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
