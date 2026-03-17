using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Practices.Commands.Writing
{
    // ============================================================
    // SUBMIT WRITING ANSWER COMMAND  (lưu từng câu — autosave)
    // ============================================================

    public record SaveWritingDraftCommand(
        Guid SessionId,
        Guid UserId,
        Guid QuestionId,
        string TextAnswer,
        int TimeSpentSeconds
    ) : IRequest<bool>;

    public class SaveWritingDraftCommandHandler
        : IRequestHandler<SaveWritingDraftCommand, bool>
    {
        private readonly IAppDbContext _context;

        public SaveWritingDraftCommandHandler(IAppDbContext context)
            => _context = context;

        public async Task<bool> Handle(
            SaveWritingDraftCommand request,
            CancellationToken cancellationToken)
        {
            var answer = await _context.PracticeAnswers
                .FirstOrDefaultAsync(a => a.PracticeAttemptId == request.SessionId
                                       && a.QuestionId == request.QuestionId,
                                    cancellationToken);

            if (answer == null) return false;

            // Chỉ cho phép autosave khi chưa submit
            if (answer.GradingStatus == GradingStatusEnum.Completed) return false;

            answer.TextAnswer = request.TextAnswer;
            answer.TimeSpentSeconds = request.TimeSpentSeconds;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
