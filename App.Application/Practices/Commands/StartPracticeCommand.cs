using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Practices.Queries;
using App.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            _context.PracticeAnswers.AddRange(practiceAnswers);
            await _context.SaveChangesAsync(cancellationToken);

            return questions;
        }

        private string GenerateTitle(List<PracticePartDto> parts)
        {
            if (parts.Count == 1)
                return $"{parts[0].PartName} Practice";

            return $"Multi-Part Practice ({string.Join(", ", parts.Select(p => $"P{p.PartNumber}"))})";
        }
    }

   


    // ============================================
    // 4. GET PRACTICE RESULT
    // ============================================

    public record GetPracticeResultQuery(Guid SessionId) : IRequest<PracticeResultDto>;

    public class GetPracticeResultQueryHandler : IRequestHandler<GetPracticeResultQuery, PracticeResultDto>
    {
        private readonly IAppDbContext _context;

        public GetPracticeResultQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PracticeResultDto> Handle(GetPracticeResultQuery request, CancellationToken cancellationToken)
        {
            var attempt = await _context.PracticeAttempts
                .Include(a => a.PartResults)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException("Practice result not found");

            if (attempt.Status == AttemptStatus.InProgress)
                throw new InvalidOperationException("Practice not submitted yet");

            return new PracticeResultDto
            {
                SessionId = attempt.Id,
                TotalQuestions = attempt.TotalQuestions,
                CorrectAnswers = attempt.CorrectAnswers,
                IncorrectAnswers = attempt.IncorrectAnswers,
                UnansweredQuestions = attempt.UnansweredQuestions,
                Score = attempt.Score,
                AccuracyPercentage = attempt.AccuracyPercentage,
                TotalTime = TimeSpan.FromSeconds(attempt.ActualTimeSeconds ?? 0),
                PartResults = attempt.PartResults.ToDictionary(
                    pr => pr.PartName,
                    pr => new PartResultDto
                    {
                        PartName = pr.PartName,
                        PartNumber = pr.PartNumber,
                        Total = pr.TotalQuestions,
                        Correct = pr.CorrectAnswers,
                        Incorrect = pr.IncorrectAnswers,
                        Unanswered = pr.UnansweredQuestions,
                        Percentage = pr.Percentage,
                        AverageTimePerQuestion = pr.AverageTimePerQuestion
                    }
                )
            };
        }
    }

    // ============================================
    // 5. GET PRACTICE HISTORY
    // ============================================

    public record GetPracticeHistoryQuery(
        Guid UserId,
        Guid? CategoryId = null,
        int PageIndex = 1,
        int PageSize = 10
    ) : IRequest<PaginatedResult<PracticeHistoryDto>>;

    public class PracticeHistoryDto
    {
        public Guid SessionId { get; set; }
        public string Title { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double AccuracyPercentage { get; set; }
        public double Score { get; set; }
        public AttemptStatus Status { get; set; }
    }

    public class GetPracticeHistoryQueryHandler : IRequestHandler<GetPracticeHistoryQuery, PaginatedResult<PracticeHistoryDto>>
    {
        private readonly IAppDbContext _context;

        public GetPracticeHistoryQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedResult<PracticeHistoryDto>> Handle(GetPracticeHistoryQuery request, CancellationToken cancellationToken)
        {
            var query = _context.PracticeAttempts
                .Where(a => a.UserId == request.UserId);

            if (request.CategoryId.HasValue)
                query = query.Where(a => a.CategoryId == request.CategoryId);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.StartedAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new PracticeHistoryDto
                {
                    SessionId = a.Id,
                    Title = a.Title,
                    StartedAt = a.StartedAt,
                    SubmittedAt = a.SubmittedAt,
                    TotalQuestions = a.TotalQuestions,
                    CorrectAnswers = a.CorrectAnswers,
                    AccuracyPercentage = a.AccuracyPercentage,
                    Score = a.Score,
                    Status = a.Status
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<PracticeHistoryDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
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