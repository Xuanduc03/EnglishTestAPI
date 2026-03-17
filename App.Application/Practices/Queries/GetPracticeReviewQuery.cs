using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Practices.Queries
{
    /// <summary>
    /// Query : xem lại chi tiết từng câu hỏi (review) sau khi nộp bài.
    /// </summary>
    public record GetPracticeReviewQuery(Guid SessionId) : IRequest<PracticeReviewDto>;

    public class GetPracticeReviewQueryHandler : IRequestHandler<GetPracticeReviewQuery, PracticeReviewDto>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetPracticeReviewQueryHandler(IAppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PracticeReviewDto> Handle(GetPracticeReviewQuery request, CancellationToken cancellationToken)
        {
            var attempt = await _context.PracticeAttempts
                .Include(a => a.Answers)
                    .ThenInclude(pa => pa.Question)
                        .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException("Không tìm thấy phiên thi");

            if (attempt.UserId != _currentUser.UserId)
                throw new UnauthorizedAccessException("Không có quyền truy cập");

            var questions = attempt.Answers.OrderBy(a => a.OrderIndex).Select(pa => new PracticeReviewQuestionDto
            {
                QuestionId = pa.QuestionId,
                Content = pa.Question.Content,
                OrderIndex = pa.OrderIndex,
                SelectedAnswerId = pa.SelectedAnswerId,
                CorrectAnswerId = pa.Question.Answers.FirstOrDefault(a => a.IsCorrect)?.Id,
                IsCorrect = pa.IsCorrect,
                Explanation = pa.Question.Explanation,
                Answers = pa.Question.Answers.OrderBy(a => a.OrderIndex).Select(a => new PracticeReviewAnswerDto
                {
                    AnswerId = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect,
                    OrderIndex = a.OrderIndex
                }).ToList()
            }).ToList();

            return new PracticeReviewDto
            {
                SessionId = attempt.Id,
                Title = attempt.Title,
                Questions = questions
            };
        }
    }
}
