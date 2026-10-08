using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.ExamAttempts.Commands
{
    /// <summary>
    /// Command : Lưu đáp án từng câu (auto-save)
    /// </summary>
    public class SaveAnswerCommand : IRequest<AnswerQuestionResult>
    {
        [Required] 
        public Guid AttemptId { get; set; }
        [Required] 
        public Guid ExamQuestionId { get; set; }
        public Guid? SelectedAnswerId { get; set; } // NULL = skip/blank
        public int? TimeSpentSeconds { get; set; } // Client tracking

        /// <summary>
        /// Token returned when an anonymous attempt is started.
        /// </summary>
        public string? GuestToken { get; set; }
    }

    public class AnswerQuestionResult
    {
        public bool Success { get; set; }
        public int TotalAnswered { get; set; } // UI show progress
        public int TotalSkipped { get; set; }
        public string? Message { get; set; }
    }

    public class SaveAnswerCommandHandler
        : IRequestHandler<SaveAnswerCommand, AnswerQuestionResult>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public SaveAnswerCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<AnswerQuestionResult> Handle(
            SaveAnswerCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Load attempt
            var attempt = await _context.ExamAttempts
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken) ??
                throw new KeyNotFoundException("Không tìm thấy phiên thi");

            // 2. Auth check
            App.Application.ExamAttempts.GuestAttemptAccess.EnsureOwner(
                attempt, _currentUserService.UserId, request.GuestToken);

            // 2. Check status đơn giản
            if (attempt.Status != ExamAttemptStatus.InProgress)
                throw new InvalidOperationException("Phiên thi không mở");
            if (attempt.ExpiresAt.HasValue && attempt.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Thời gian làm bài đã hết");

            // 3. Load answer record
            var examAnswer = await _context.ExamAnswers
                .FirstOrDefaultAsync(a =>
                    a.ExamAttemptId == request.AttemptId &&
                    a.ExamQuestionId == request.ExamQuestionId,
                    cancellationToken);

            if (examAnswer == null)
                throw new KeyNotFoundException("Không có câu hỏi trong phiên thi");

            // 4. Update answer
            examAnswer.SelectedAnswerId = request.SelectedAnswerId;
            examAnswer.IsAnswered = request.SelectedAnswerId.HasValue;

            if (request.TimeSpentSeconds.HasValue)
                examAnswer.TimeSpentSeconds = request.TimeSpentSeconds.Value;

            await _context.SaveChangesAsync(cancellationToken);

            var progress = await _context.ExamAnswers
               .Where(a => a.ExamAttemptId == request.AttemptId)
               .GroupBy(_ => 1)
               .Select(g => new
               {
                   Total = g.Count(),
                   Answered = g.Count(a => a.IsAnswered)
               })
               .FirstOrDefaultAsync(cancellationToken);

            return new AnswerQuestionResult
            {
                Success = true,
                TotalAnswered = progress?.Answered ?? 0,
                TotalSkipped = (progress?.Total ?? 0) - (progress?.Answered ?? 0),
                Message = "Answer saved"
            };
        }
    }
}
