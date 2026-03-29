using App.Application.DTOs;
using App.Application.DTOs.Questions;
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
                        .ThenInclude(q => q.Media)
                .Include(a => a.Answers)
                    .ThenInclude(pa => pa.Question)
                        .ThenInclude(q => q.Answers)
                .Include(a => a.Answers)
                    .ThenInclude(pa => pa.Question)
                        .ThenInclude(q => q.Group)
                            .ThenInclude(g => g.Media)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException("Không tìm thấy phiên thi");

            if (attempt.UserId != _currentUser.UserId)
                throw new UnauthorizedAccessException("Không có quyền truy cập");

            var orderedAnswers = attempt.Answers.OrderBy(a => a.OrderIndex).ToList();

            // Gom nhóm câu hỏi để lấy metadata
            var groupMeta = orderedAnswers
                .Where(a => a.Question.GroupId.HasValue)
                .GroupBy(a => a.Question.GroupId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.OrderIndex).Select(a => a.Question).ToList());

            var questions = orderedAnswers.Select(pa =>
            {
                var q = pa.Question;
                var dto = new PracticeReviewQuestionDto
                {
                    QuestionId = q.Id,
                    Content = q.Content,
                    OrderIndex = pa.OrderIndex,
                    SelectedAnswerId = pa.SelectedAnswerId,
                    CorrectAnswerId = q.Answers.FirstOrDefault(a => a.IsCorrect)?.Id,
                    IsCorrect = pa.IsCorrect,
                    Explanation = q.Explanation,
                    Media = q.Media?.OrderBy(m => m.OrderIndex).Select(MapMedia).ToList() ?? new List<PracticeMediaDto>(),
                    Answers = q.Answers.OrderBy(a => a.OrderIndex).Select(MapAnswer).ToList()
                };

                // Thêm thông tin nhóm nếu có
                if (q.GroupId.HasValue && q.Group != null && groupMeta.TryGetValue(q.GroupId.Value, out var siblings))
                {
                    dto.GroupId = q.GroupId;
                    dto.GroupContent = q.Group.Content;
                    dto.TotalQuestionsInGroup = siblings.Count;
                    dto.QuestionIndexInGroup = siblings.FindIndex(x => x.Id == q.Id) + 1;
                    dto.GroupMedia = q.Group.Media?.OrderBy(m => m.OrderIndex).Select(MapGroupMedia).ToList();
                }

                // Tính HasAudio/HasImage từ media của câu hỏi
                dto.HasAudio = dto.Media.Any(m => IsAudio(m.Type));
                dto.HasImage = dto.Media.Any(m => IsImage(m.Type));
                dto.AudioUrl = dto.Media.FirstOrDefault(m => IsAudio(m.Type))?.Url;
                dto.ImageUrl = dto.Media.FirstOrDefault(m => IsImage(m.Type))?.Url;

                // Passages (dự phòng cho Part 7)
                dto.Passages = new List<GroupPassageDto>();

                return dto;
            }).ToList();

            return new PracticeReviewDto
            {
                SessionId = attempt.Id,
                Title = attempt.Title,
                Questions = questions
            };
        }

        private PracticeMediaDto MapMedia(QuestionMedia m)
        {
            return new PracticeMediaDto
            {
                Id = m.Id,
                Url = m.Url,
                Type = ResolveMediaType(m.MediaType, m.Url) // sửa m.Type -> m.MediaType
            };
        }

        private PracticeMediaDto MapGroupMedia(QuestionGroupMedia m)
        {
            return new PracticeMediaDto
            {
                Id = m.Id,
                Url = m.Url,
                Type = ResolveMediaType(m.MediaType, m.Url)
            };
        }

        private PracticeReviewAnswerDto MapAnswer(Answer a)
        {
            return new PracticeReviewAnswerDto
            {
                AnswerId = a.Id,
                Content = a.Content,
                IsCorrect = a.IsCorrect,
                OrderIndex = a.OrderIndex,
            };
        }

        private string ResolveMediaType(string? mediaType, string? url)
        {
            if (!string.IsNullOrWhiteSpace(mediaType))
                return mediaType.ToLower();

            if (string.IsNullOrEmpty(url)) return "unknown";
            var u = url.ToLower();
            if (u.EndsWith(".mp3") || u.EndsWith(".wav") || u.EndsWith(".ogg") || u.EndsWith(".m4a")) return "audio";
            if (u.EndsWith(".jpg") || u.EndsWith(".jpeg") || u.EndsWith(".png") || u.EndsWith(".webp")) return "image";
            if (u.EndsWith(".mp4") || u.EndsWith(".webm")) return "video";
            if (u.Contains("/image/upload/")) return "image";
            return "unknown";
        }

        private bool IsAudio(string type) => type == "audio";
        private bool IsImage(string type) => type == "image";
    }
}
