using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.ExamAttempts.Commands.IELTS
{
    public class IeltsSaveFillInAnswerCommand : IRequest<bool>
    {
        public Guid AttemptId { get; set; }
        public Guid ExamQuestionId { get; set; }
        public Guid UserId { get; set; }
        public string? GuestToken { get; set; }
        public string TextAnswer { get; set; } = string.Empty;
        public int? TimeSpentSeconds { get; set; }
    }

    public class IeltsSaveFillInAnswerCommandHandler
    : IRequestHandler<IeltsSaveFillInAnswerCommand, bool>
    {
        private readonly IAppDbContext _context;
        public IeltsSaveFillInAnswerCommandHandler(IAppDbContext context) => _context = context;

        public async Task<bool> Handle(IeltsSaveFillInAnswerCommand request, CancellationToken ct)
        {
            var ea = await _context.ExamAnswers
                .FirstOrDefaultAsync(a =>
                    a.ExamAttemptId == request.AttemptId &&
                    a.ExamQuestionId == request.ExamQuestionId, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy câu trả lời");

            // Validate attempt còn InProgress
            var attempt = await _context.ExamAttempts
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy phiên thi");

            App.Application.ExamAttempts.GuestAttemptAccess.EnsureOwner(
                attempt, request.UserId == Guid.Empty ? null : request.UserId, request.GuestToken);
            if (attempt.Status != ExamAttemptStatus.InProgress)
                throw new InvalidOperationException("Phiên thi đã kết thúc");
            if (attempt.ExpiresAt.HasValue && attempt.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Thời gian làm bài đã hết");

            ea.TextAnswer = request.TextAnswer.Trim();
            ea.IsAnswered = !string.IsNullOrWhiteSpace(request.TextAnswer);
            ea.TimeSpentSeconds = request.TimeSpentSeconds;
            ea.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }

}
