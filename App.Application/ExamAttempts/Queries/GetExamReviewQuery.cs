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

namespace App.Application.ExamAttempts.Queries
{
    // ============================================
    // 2. GET EXAM REVIEW
    // GET /api/exam-attempts/{attemptId}/review
    // ============================================

    public record GetExamReviewQuery(Guid AttemptId) : IRequest<ExamReviewDto>;

    public class GetExamReviewQueryHandler
        : IRequestHandler<GetExamReviewQuery, ExamReviewDto>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetExamReviewQueryHandler(
            IAppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<ExamReviewDto> Handle(
            GetExamReviewQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Load attempt
            var attempt = await _context.ExamAttempts
                .AsNoTracking()
                .Include(a => a.Exam)
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId && !a.IsDeleted, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy phiên thi");

            // 2. Auth check
            if (attempt.UserId != _currentUser.UserId)
                throw new UnauthorizedAccessException("Không có quyền truy cập phiên thi này");

            // 3. Chỉ cho review sau khi đã nộp
            if (attempt.Status == ExamAttemptStatus.InProgress)
                throw new InvalidOperationException("Bài thi chưa được nộp");

            // 4. Load answers với đầy đủ data
            var answers = await _context.ExamAnswers
                .AsNoTracking()
                .Where(a => a.ExamAttemptId == request.AttemptId && !a.IsDeleted)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.ExamSection)
                        .ThenInclude(s => s.Category)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Media)
                .OrderBy(a => a.ExamQuestions.ExamSection.OrderIndex)
                    .ThenBy(a => a.ExamQuestions.OrderIndex)
                .ToListAsync(cancellationToken);

            // 5. Group theo section
            var sections = answers
                .GroupBy(a => new
                {
                    SectionId = a.ExamQuestions.ExamSectionId,
                    SectionName = a.ExamQuestions.ExamSection?.Category?.Name ?? "Unknown",
                    OrderIndex = a.ExamQuestions.ExamSection?.OrderIndex ?? 0,
                })
                .OrderBy(g => g.Key.OrderIndex)
                .Select(g => new ExamReviewSectionDto
                {
                    SectionId = g.Key.SectionId,
                    SectionName = g.Key.SectionName,
                    OrderIndex = g.Key.OrderIndex,
                    Questions = g.Select(a => new ExamReviewQuestionDto
                    {
                        ExamAnswerId = a.Id,
                        QuestionId = a.QuestionId,
                        OrderIndex = a.ExamQuestions.OrderIndex,
                        Point = (double)a.ExamQuestions.Point,
                        Content = a.ExamQuestions.Question?.Content ?? "",
                        QuestionType = a.ExamQuestions.Question?.QuestionType.ToString() ?? "",
                        Explanation = a.ExamQuestions.Question?.Explanation,
                        AudioUrl = a.ExamQuestions.Question?.Media?
                            .FirstOrDefault(m => m.MediaType == "audio")?.Url,
                        ImageUrl = a.ExamQuestions.Question?.Media?
                            .FirstOrDefault(m => m.MediaType == "image")?.Url,

                        // Trắc nghiệm
                        SelectedAnswerId = a.SelectedAnswerId,
                        CorrectAnswerId = a.CorrectAnswerId,
                        IsCorrect = a.IsCorrect,
                        IsAnswered = a.IsAnswered,
                        Answers = a.ExamQuestions.Question?.Answers
                            .OrderBy(ans => ans.OrderIndex)
                            .Select(ans => new ExamReviewAnswerDto
                            {
                                Id = ans.Id,
                                Content = ans.Content ?? "",
                                IsCorrect = ans.IsCorrect,
                                OrderIndex = ans.OrderIndex,
                            }).ToList() ?? new(),

                        // Writing/Speaking
                        TextAnswer = a.TextAnswer,
                        AiFeedback = a.AiFeedback,
                        AiScoreDetailJson = a.AiScoreDetailJson,
                        IsAiGraded = a.IsAiGraded,
                        GradingStatus = a.GradingStatus.ToString(),
                    })
                    .OrderBy(q => q.OrderIndex)
                    .ToList()
                })
                .ToList();

            return new ExamReviewDto
            {
                AttemptId = attempt.Id,
                ExamId = attempt.ExamId,
                ExamTitle = attempt.Exam?.Title ?? "Unknown",
                ExamCode = attempt.Exam?.Code ?? "",
                SubmittedAt = attempt.SubmitedAt ?? DateTime.UtcNow,
                TotalScore = attempt.TotalScore,
                TotalQuestions = attempt.TotalQuestions,
                CorrectAnswers = attempt.CorrectAnswers,
                Sections = sections,
            };
        }
    }
}
