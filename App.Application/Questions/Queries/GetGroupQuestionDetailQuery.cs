using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
                .Include(g => g.Category)
                .Include(g => g.Difficulty)
                .Include(g => g.Media)
                .Include(g => g.Tags)
                .Include(g => g.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.Answers.OrderBy(a => a.OrderIndex))
                .Include(g => g.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.Media.OrderBy(m => m.OrderIndex))
                .FirstOrDefaultAsync(g =>
                    g.Id == request.Id && !g.IsDeleted,
                    cancellationToken)
                ?? throw new KeyNotFoundException($"Nhóm câu hỏi {request.Id} không tồn tại");

            return new QuestionGroupDetailDto
            {
                Id = group.Id,
                CategoryId = group.CategoryId,
                CategoryName = group.Category?.Name,
                CategoryCode = group.Category?.Code,

                Content = group.Content,
                Explanation = group.Explanation,
                Transcript = group.Transcript,
                MediaJson = group.MediaJson,

                DifficultyId = group.DifficultyId,
                DifficultyName = group.Difficulty?.Name,

                IsActive = group.IsActive,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,

                Media = group.Media
                    .OrderBy(m => m.OrderIndex)
                    .Select(m => new MediaDto
                    {
                        Id = m.Id,
                        Url = m.Url,
                        MediaType = m.MediaType,
                        OrderIndex = m.OrderIndex,
                    })
                    .ToList(),

                Questions = group.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new GroupQuestionItemDto
                    {
                        Id = q.Id,
                        Content = q.Content,
                        QuestionType = q.QuestionType,
                        PromptTypes = q.PromptTypes,
                        DifficultyId = q.DifficultyId,
                        DefaultScore = q.DefaultScore,
                        Explanation = q.Explanation,
                        OrderIndex = q.OrderIndex,

                        // ── IELTS fill-in fields
                        IsAiGraded = q.IsAiGraded,
                        SampleAnswer = q.SampleAnswer,
                        MinWords = q.MinWords,
                        MaxWords = q.MaxWords,

                        Media = q.Media
                            .OrderBy(m => m.OrderIndex)
                            .Select(m => new MediaDto
                            {
                                Id = m.Id,
                                Url = m.Url,
                                MediaType = m.MediaType,
                                OrderIndex = m.OrderIndex,
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
                                OrderIndex = a.OrderIndex,
                            })
                            .ToList(),
                    })
                    .ToList(),
            };
        }
    }
}