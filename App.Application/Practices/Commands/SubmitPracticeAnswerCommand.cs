using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Practices.Commands
{
    // ============================================
    // 2. SUBMIT ANSWER
    // ============================================

    /// <summary>
    /// Submit câu trả lời cho 1 question
    /// </summary>
    public record SubmitPracticeAnswerCommand(
        Guid SessionId,
        Guid QuestionId,
        Guid? AnswerId,
        bool? IsMarkedForReview,
        int TimeSpentSeconds
    ) : IRequest<SubmitAnswerResult>;

    public class SubmitAnswerResult
    {
        public bool IsCorrect { get; set; }
        public Guid? CorrectAnswerId { get; set; }
        public string? Explanation { get; set; }
    }

    public class SubmitPracticeAnswerCommandHandler : IRequestHandler<SubmitPracticeAnswerCommand, SubmitAnswerResult>
    {
        private readonly IAppDbContext _context;

        public SubmitPracticeAnswerCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<SubmitAnswerResult> Handle(SubmitPracticeAnswerCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm PracticeAnswer record
            var practiceAnswer = await _context.PracticeAnswers
                .Include(pa => pa.Question)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(
                    pa => pa.PracticeAttemptId == request.SessionId
                       && pa.QuestionId == request.QuestionId,
                    cancellationToken
                );

            if (practiceAnswer == null)
                throw new KeyNotFoundException("Practice answer not found");

            // 2. Tìm correct answer
            var correctAnswer = practiceAnswer.Question.Answers
                .FirstOrDefault(a => a.IsCorrect);

            // 3. Update answer
            practiceAnswer.SelectedAnswerId = request.AnswerId;
            practiceAnswer.IsCorrect = request.AnswerId == correctAnswer?.Id;
            practiceAnswer.AnsweredAt = DateTime.UtcNow;
            practiceAnswer.TimeSpentSeconds = request.TimeSpentSeconds;
            practiceAnswer.IsMarkedForReview = request.IsMarkedForReview ?? false;

            // Track changes
            if (practiceAnswer.ChangeCount == null)
                practiceAnswer.ChangeCount = 0;
            practiceAnswer.ChangeCount++;

            await _context.SaveChangesAsync(cancellationToken);

            // 4. Return result
            return new SubmitAnswerResult
            {
                IsCorrect = practiceAnswer.IsCorrect,
                CorrectAnswerId = correctAnswer?.Id,
                Explanation = correctAnswer?.Explanation
            };
        }
    }
}
