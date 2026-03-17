using MediatR;
using global::App.Application.DTOs.App.Application.DTOs;
using global::App.Application.Interfaces;
using global::App.Application.Writing.Queries;
using global::App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Practices.Commands
{
    // ============================================================
    // START WRITING PRACTICE COMMAND
    // Tạo PracticeAttempt mới cho Writing session
    // Tương tự StartPracticeCommand nhưng:
    //   - Không tạo PracticeAnswer cho multiple choice
    //   - Tạo PracticeAnswer rỗng với TextAnswer = null
    //   - Không cần SelectedAnswerId
    // ============================================================

    public record StartWritingPracticeCommand(
        Guid UserId,
        List<Guid> CategoryIds,        // Writing Part categories
        bool IsTimed = true,
        int? TimeLimitMinutes = 60     // Tổng 60 phút theo chuẩn TOEIC
    ) : IRequest<WritingSessionDto>;

    public class StartWritingPracticeCommandHandler
        : IRequestHandler<StartWritingPracticeCommand, WritingSessionDto>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;

        public StartWritingPracticeCommandHandler(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<WritingSessionDto> Handle(
            StartWritingPracticeCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Kiểm tra user không có session đang dở cho các part này
            await CancelStaleAttemptsAsync(request.UserId, request.CategoryIds, cancellationToken);

            // 2. Lấy câu hỏi qua Query
            var session = await _mediator.Send(
                new GetWritingQuestionsQuery(request.CategoryIds, RandomOrder: true),
                cancellationToken);

            if (session.Parts.Count == 0)
                throw new InvalidOperationException("No writing questions available for the selected parts.");

            // 3. Tạo PracticeAttempt
            var attempt = new PracticeAttempt
            {
                Id = session.SessionId,
                UserId = request.UserId,
                // CategoryId null nếu luyện nhiều part
                CategoryId = request.CategoryIds.Count == 1 ? request.CategoryIds[0] : null,
                Title = session.Title,
                StartedAt = DateTime.UtcNow,
                TimeLimitSeconds = request.IsTimed
                    ? (request.TimeLimitMinutes * 60 ?? session.TimeLimitSeconds)
                    : null,
                Status = AttemptStatus.InProgress,
                TotalQuestions = session.TotalQuestions,
                IsRandomOrder = true,
                AttemptType = AttemptType.Writing   // Phân biệt với Practice thường
            };

            _context.PracticeAttempts.Add(attempt);

            // 4. Tạo PracticeAnswer rỗng cho mỗi câu (TextAnswer = null, không có SelectedAnswerId)
            var answers = new List<PracticeAnswer>();
            int orderIndex = 1;

            foreach (var part in session.Parts)
            {
                foreach (var question in part.Questions)
                {
                    answers.Add(new PracticeAnswer
                    {
                        Id = Guid.NewGuid(),
                        PracticeAttemptId = attempt.Id,
                        QuestionId = question.QuestionId,
                        OrderIndex = orderIndex++,
                        TextAnswer = null,
                        IsCorrect = false,
                        GradingStatus = GradingStatusEnum.NotRequired, // Chờ submit mới grade
                        TimeSpentSeconds = 0,
                        IsMarkedForReview = false
                    });
                }

                // 5. Tạo PracticePartResult tracking
                _context.PracticePartResults.Add(new PracticePartResult
                {
                    Id = Guid.NewGuid(),
                    PracticeAttemptId = attempt.Id,
                    CategoryId = part.PartId,
                    PartNumber = part.PartNumber,
                    PartName = part.PartName,
                    TotalQuestions = part.Questions.Count,
                    CorrectAnswers = 0,
                    IncorrectAnswers = 0,
                    UnansweredQuestions = part.Questions.Count,
                    Percentage = 0,
                    TotalTimeSeconds = 0
                });
            }

            try
            {
                _context.PracticeAnswers.AddRange(answers);
                await _context.SaveChangesAsync(cancellationToken);
                return session;
            }
            catch (DbUpdateException ex)
            {
                throw new Exception($"Failed to start writing session: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        /// <summary>
        /// Hủy các attempt Writing đang InProgress cũ của user cho cùng bộ category
        /// Tránh duplicate session khi user refresh/tạo lại
        /// </summary>
        private async Task CancelStaleAttemptsAsync(
            Guid userId,
            List<Guid> categoryIds,
            CancellationToken cancellationToken)
        {
            if (categoryIds.Count == 0) return;

            // Lấy attempt InProgress cũ hơn 24h → tự động Abandoned
            var cutoff = DateTime.UtcNow.AddHours(-24);

            var staleAttempts = await _context.PracticeAttempts
                .Where(a => a.UserId == userId
                         && a.AttemptType == AttemptType.Writing
                         && a.Status == AttemptStatus.InProgress
                         && a.StartedAt < cutoff)
                .ToListAsync(cancellationToken);

            foreach (var stale in staleAttempts)
                stale.Status = AttemptStatus.Abandoned;

            if (staleAttempts.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
