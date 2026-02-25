using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Practices.Queries;
using App.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Practice.Commands
{
    // ============================================
    // 1. START PRACTICE SESSION
    // ============================================

    /// <summary>
    /// Tạo session practice mới và lấy câu hỏi
    /// </summary>
    public record StartPracticeCommand(
        Guid UserId,
        List<Guid> CategoryIds,
        int QuestionsPerPart,
        bool IsTimed,
        int? TimeLimitMinutes
    ) : IRequest<PracticeSessionDto>;

    public class StartPracticeCommandHandler : IRequestHandler<StartPracticeCommand, PracticeSessionDto>
    {
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public StartPracticeCommandHandler(IAppDbContext context, IMapper mapper, IMediator mediator)
        {
            _context = context;
            _mapper = mapper;
            _mediator = mediator;
        }

        public async Task<PracticeSessionDto> Handle(StartPracticeCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy câu hỏi
            var questions = await _mediator.Send(new GetPracticeByPartQuery(
                request.CategoryIds,
                request.QuestionsPerPart,
                RandomOrder: true
            ), cancellationToken);

            // 2. Tạo PracticeAttempt
            var attempt = new PracticeAttempt
            {
                Id = questions.SessionId,  // Dùng SessionId từ query
                UserId = request.UserId,
                CategoryId = request.CategoryIds.Count == 1 ? request.CategoryIds[0] : null,
                Title = GenerateTitle(questions.Parts),
                StartedAt = DateTime.UtcNow,
                TimeLimitSeconds = request.IsTimed ? request.TimeLimitMinutes * 60 : null,
                Status = AttemptStatus.InProgress,
                TotalQuestions = questions.TotalQuestions,
                IsRandomOrder = true
            };

            _context.PracticeAttempts.Add(attempt);

            // 3. Tạo PracticeAnswer records (empty)
            var practiceAnswers = new List<PracticeAnswer>();
            int orderIndex = 1;

            foreach (var part in questions.Parts)
            {
                foreach (var question in part.Questions)
                {
                    practiceAnswers.Add(new PracticeAnswer
                    {
                        Id = Guid.NewGuid(),
                        PracticeAttemptId = attempt.Id,
                        QuestionId = question.QuestionId,
                        OrderIndex = orderIndex++,
                        IsCorrect = false,
                        IsMarkedForReview = false,
                        TimeSpentSeconds = 0
                    });
                }

                // 4. Tạo PracticePartResult
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
                _context.PracticeAnswers.AddRange(practiceAnswers);
                await _context.SaveChangesAsync(cancellationToken);
                return questions;
            }
            catch (DbUpdateException ex)
            {
                // Log inner exception để thấy đúng constraint nào bị vi phạm
                throw new Exception($"SaveChanges failed: {ex.InnerException?.Message ?? ex.Message}");
            }

            
        }

        private string GenerateTitle(List<PracticePartDto> parts)
        {
            if (parts.Count == 1)
                return $"{parts[0].PartName} Practice";

            return $"Multi-Part Practice ({string.Join(", ", parts.Select(p => $"P{p.PartNumber}"))})";
        }
    }

}

// ============================================
// PAGINATED RESULT
// ============================================

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;
}