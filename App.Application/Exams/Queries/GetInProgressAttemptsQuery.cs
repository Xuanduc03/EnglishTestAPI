using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Exams.Queries
{
    /// <summary>
    /// Quert : Để lấy danh sách các bài thi đang làm dở (InProgress) của người dùng
    /// </summary>
    public record GetInProgressAttemptsQuery : IRequest<List<InProgressAttemptDto>>;

    public class GetInProgressAttemptsQueryHandler : IRequestHandler<GetInProgressAttemptsQuery, List<InProgressAttemptDto>>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public GetInProgressAttemptsQueryHandler(IAppDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<List<InProgressAttemptDto>> Handle(GetInProgressAttemptsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            var attempts = await _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Sections)
                        .ThenInclude(s => s.ExamQuestions)
                .Where(a => a.UserId == userId && a.Status == ExamAttemptStatus.InProgress)
                .OrderByDescending(a => a.UpdatedAt)
                .ToListAsync(cancellationToken);

            return attempts.Select(a => new InProgressAttemptDto
            {
                AttemptId = a.Id,
                ExamId = a.ExamId,
                ExamTitle = a.Exam.Title,
                Progress = CalculateProgress(a),
                StartedAt = a.StartedAt,
                LastUpdated = a.UpdatedAt
            }).ToList();
        }

        private double CalculateProgress(ExamAttempt attempt)
        {
            var totalQuestions = attempt.Exam?.Sections?.Sum(s => s.ExamQuestions?.Count ?? 0) ?? 0;
            if (totalQuestions == 0) return 0;

            var answeredCount = attempt.Answers?.Count ?? 0;
            return Math.Round((double)answeredCount / totalQuestions * 100, 2);
        }
    }
}
