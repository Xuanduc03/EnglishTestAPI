using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Practices.Commands
{
    // Command chỉ cần SessionId
    public record AbandonPracticeCommand(Guid SessionId) : IRequest<bool>;

    public class AbandonPracticeCommandHandler : IRequestHandler<AbandonPracticeCommand, bool>
    {
        private readonly IAppDbContext _context;

        public AbandonPracticeCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(AbandonPracticeCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm bài thi
            var attempt = await _context.PracticeAttempts
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException($"Không tìm thấy bài thi với ID {request.SessionId}");

            // 2. Validate: Chỉ cho phép hủy nếu đang InProgress
            if (attempt.Status == AttemptStatus.Submitted)
                throw new InvalidOperationException("Bài thi đã nộp rồi, không thể hủy bỏ.");

            if (attempt.Status == AttemptStatus.Abandoned)
                return true; // Đã hủy rồi thì return true luôn

            // 3. Cập nhật trạng thái
            attempt.Status = AttemptStatus.Abandoned;

            // Có thể muốn update thời gian kết thúc dù không tính điểm
            // attempt.SubmittedAt = DateTime.UtcNow; 

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
