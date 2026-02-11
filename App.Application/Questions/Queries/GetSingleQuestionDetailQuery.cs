using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Questions.Queries
{
    public record GetSingleQuestionDetailQuery(Guid id) : IRequest<SingleQuestionDetailDto>;

    public class GetSingleQuestionDetailQueryHandler : IRequestHandler<GetSingleQuestionDetailQuery, SingleQuestionDetailDto>
    {
        private readonly IAppDbContext _context;

        public GetSingleQuestionDetailQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<SingleQuestionDetailDto> Handle(
            GetSingleQuestionDetailQuery request,
            CancellationToken cancellationToken)
        {
            var question = await _context.Questions
                .AsNoTracking()
                .Include(q => q.Media)
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q =>
                    q.Id == request.id &&
                    q.GroupId == null &&
                    !q.IsDeleted,
                    cancellationToken);

            if (question == null)
                throw new KeyNotFoundException("Câu hỏi không tồn tại hoặc không phải câu hỏi đơn");

            return new SingleQuestionDetailDto
            {
                Id = question.Id,
                CategoryId = question.CategoryId,
                QuestionType = question.QuestionType,
                DifficultyId = question.DifficultyId,
                DefaultScore = question.DefaultScore,
                ShuffleAnswers = question.ShuffleAnswers,
                IsActive = question.IsActive,
                Content = question.Content,
                Explanation = question.Explanation,

                Media = question.Media
                    .OrderBy(m => m.OrderIndex)
                    .Select(m => new MediaDto
                    {
                        Id = m.Id,
                        Url = m.Url,
                        MediaType = m.MediaType,
                        OrderIndex = m.OrderIndex
                    })
                    .ToList(),

                Answers = question.Answers
                    .OrderBy(a => a.OrderIndex)
                    .Select(a => new AnswerDto
                    {
                        Id = a.Id,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect,
                        Feedback = a.Feedback,
                        OrderIndex = a.OrderIndex,
                    })
                    .ToList()
            };
        }
    }
}
