using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace App.Application.Questions.Queries
{
    public record GetQuestionGroupDetailQuery(Guid Id)
        : IRequest<QuestionGroupDetailDto>;

    public class GetQuestionGroupDetailQueryHandler
       : IRequestHandler<GetQuestionGroupDetailQuery, QuestionGroupDetailDto>
    {
        private readonly IAppDbContext _context;

        public GetQuestionGroupDetailQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionGroupDetailDto> Handle(
            GetQuestionGroupDetailQuery request,
            CancellationToken cancellationToken)
        {
            var group = await _context.QuestionGroups
                .AsNoTracking()
                .Include(g => g.Media)
                .Include(g => g.Questions)
                    .ThenInclude(q => q.Answers)
                .Include(g => g.Questions)
                    .ThenInclude(q => q.Media)
                .FirstOrDefaultAsync(g =>
                    g.Id == request.Id &&
                    !g.IsDeleted,
                    cancellationToken);
            // debug
            foreach(var q in group.Questions)
            {
                Debug.WriteLine($"[test QUERY] Q {q.Id} - Answers count = {q.Answers.Count}");
            }

            if (group == null)
                throw new KeyNotFoundException("Nhóm câu hỏi không tồn tại");

            return new QuestionGroupDetailDto
            {
                Id = group.Id,
                CategoryId = group.CategoryId,
                Content = group.Content,
                Explanation = group.Explanation,
                Transcript = group.Transcript,
                MediaJson = group.MediaJson,
                IsActive = group.IsActive,

                Media = group.Media
                    .OrderBy(m => m.OrderIndex)
                    .Select(m => new MediaDto
                    {
                        Id = m.Id,
                        Url = m.Url,
                        MediaType = m.MediaType,
                        OrderIndex = m.OrderIndex
                    })
                    .ToList(),

                Questions = group.Questions
                    .Where(q => !q.IsDeleted)
                    .OrderBy(q => q.CreatedAt)
                    .Select(q => new GroupQuestionItemDto
                    {
                        Id = q.Id,
                        Content = q.Content,
                        QuestionType = q.QuestionType,
                        DifficultyId = q.DifficultyId,
                        DefaultScore = q.DefaultScore,
                        Explanation = q.Explanation,

                        Media = q.Media
                            .OrderBy(m => m.OrderIndex)
                            .Select(m => new MediaDto
                            {
                                Id = m.Id,
                                Url = m.Url,
                                MediaType = m.MediaType,
                                OrderIndex = m.OrderIndex
                            })
                            .ToList(),

                        Answers = q.Answers
                            .OrderBy(a => a.OrderIndex)
                            .Select(a => new AnswerDto
                            {
                                Id = a.Id,
                                Content = a.Content,
                                IsCorrect = a.IsCorrect,
                                Feedback = a.Feedback,
                                OrderIndex = a.OrderIndex
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }

}
