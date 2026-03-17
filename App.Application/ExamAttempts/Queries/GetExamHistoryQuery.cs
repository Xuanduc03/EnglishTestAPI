using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.ExamAttempts.Queries
{
    // ============================================
    // 1. GET EXAM HISTORY
    // GET /api/exam-attempts/history
    // ============================================

    public class GetExamHistoryQuery : IRequest<PaginatedExamHistoryResult>
    {
        public Guid UserId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Status { get; set; }  // filter theo status
    }

    public class PaginatedExamHistoryResult
    {
        public List<ExamAttemptHistoryDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class GetExamHistoryQueryHandler
        : IRequestHandler<GetExamHistoryQuery, PaginatedExamHistoryResult>
    {
        private readonly IAppDbContext _context;

        public GetExamHistoryQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedExamHistoryResult> Handle(
            GetExamHistoryQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.UserId == request.UserId && !a.IsDeleted)
                .Include(a => a.Exam)
                .Include(a => a.SectionResults)
                    .ThenInclude(sr => sr.Section)
                        .ThenInclude(s => s.Category)
                .AsQueryable();

            // Filter theo status
            if (!string.IsNullOrWhiteSpace(request.Status)
                && Enum.TryParse<ExamAttemptStatus>(request.Status, true, out var statusEnum))
            {
                query = query.Where(a => a.Status == statusEnum);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(a => a.StartedAt)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = items.Select(a => new ExamAttemptHistoryDto
            {
                AttemptId = a.Id,
                ExamId = a.ExamId,
                ExamTitle = a.Exam?.Title ?? "Unknown",
                ExamCode = a.Exam?.Code ?? "",
                StartedAt = a.StartedAt,
                SubmittedAt = a.SubmitedAt,
                ActualTimeSeconds = a.ActualTimeSeconds,
                Status = a.Status.ToString(),
                TotalScore = a.TotalScore,
                ListeningScore = a.ListeningScore,
                ReadingScore = a.ReadingScore,
                TotalQuestions = a.TotalQuestions,
                CorrectAnswers = a.CorrectAnswers,
                IncorrectAnswers = a.IncorrectAnswers,
                UnansweredQuestions = a.UnanswerQuestions,
                AccuracyPercent = a.TotalQuestions > 0
                    ? Math.Round((double)a.CorrectAnswers / a.TotalQuestions * 100, 1)
                    : 0,
                Sections = a.SectionResults?.Select(sr => new ExamSectionSummaryDto
                {
                    SectionId = sr.ExamSectionId,
                    SectionName = sr.Section?.Category?.Name ?? "Unknown",
                    TotalQuestions = sr.TotalQuestions,
                    CorrectAnswers = sr.CorrectAnswers,
                    AccuracyPercent = sr.TotalQuestions > 0
                        ? Math.Round((double)sr.CorrectAnswers / sr.TotalQuestions * 100, 1)
                        : 0,
                }).ToList() ?? new()
            }).ToList();

            return new PaginatedExamHistoryResult
            {
                Items = dtos,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }
    }
}
