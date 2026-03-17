using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Practices.Commands
{
    /// <summary>
    /// Lưu tiến độ khi user thoát giữa chừng.
    /// GIỮ status = InProgress để còn Resume được.
    /// </summary>
    public record AbandonPracticeCommand(
        Guid SessionId,
        List<SubmitAnswerItem> Answers,
        int TotalTimeSeconds
    ) : IRequest<bool>;

    public class AbandonPracticeCommandHandler : IRequestHandler<AbandonPracticeCommand, bool>
    {
        private readonly IAppDbContext _context;

        public AbandonPracticeCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(AbandonPracticeCommand request, CancellationToken cancellationToken)
        {
            // Không AsNoTracking — cần EF track để SaveChanges có hiệu lực
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers)
                    .ThenInclude(pa => pa.Question)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException($"Không tìm thấy bài thi với ID {request.SessionId}");

            if (attempt.Status == AttemptStatus.Submitted)
                throw new InvalidOperationException("Bài thi đã nộp rồi, không thể cập nhật.");

            // Lưu progress từng câu đã trả lời
            var answerLookup = request.Answers.ToDictionary(a => a.QuestionId);

            foreach (var practiceAnswer in attempt.Answers)
            {
                if (!answerLookup.TryGetValue(practiceAnswer.QuestionId, out var submitted))
                    continue; // chưa trả lời → giữ nguyên

                practiceAnswer.SelectedAnswerId = submitted.AnswerId;
                practiceAnswer.IsMarkedForReview = submitted.IsMarkedForReview;

                if (submitted.AnswerId.HasValue && practiceAnswer.AnsweredAt == null)
                    practiceAnswer.AnsweredAt = DateTime.UtcNow;

                if (submitted.AnswerId.HasValue)
                {
                    var correct = practiceAnswer.Question?.Answers.FirstOrDefault(a => a.IsCorrect);
                    practiceAnswer.IsCorrect = correct != null && submitted.AnswerId == correct.Id;
                }
                else
                {
                    practiceAnswer.IsCorrect = false;
                }
            }

            attempt.ActualTimeSeconds = request.TotalTimeSeconds;
            attempt.UpdatedAt = DateTime.UtcNow;

            // ✅ KHÔNG đổi Status → giữ InProgress để Resume được
            // Nếu cần "bỏ hẳn" thì tạo endpoint/command riêng: DiscardPracticeCommand

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}