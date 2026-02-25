using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Practices.Queries
{
    public record GetPracticeSessionQuery(Guid SessionId) : IRequest<PracticeSessionDto>;

    public class GetPracticeSessionQueryHandler : IRequestHandler<GetPracticeSessionQuery, PracticeSessionDto>
    {
        private readonly IAppDbContext _context;

        public GetPracticeSessionQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PracticeSessionDto> Handle(GetPracticeSessionQuery request, CancellationToken cancellationToken)
        {
            // 1. Load attempt
            var attempt = await _context.PracticeAttempts
                .AsNoTracking()
                .Include(a => a.Answers)
                .Include(a => a.PartResults)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId, cancellationToken);

            if (attempt == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên làm bài với ID {request.SessionId}");

            if (attempt.Status == AttemptStatus.Submitted)
                throw new InvalidOperationException("Bài tập này đã hoàn thành, không thể làm tiếp.");

            // 2. Load questions theo đúng thứ tự
            var answerData = attempt.Answers.OrderBy(a => a.OrderIndex).ToList();
            var questionIds = answerData.Select(a => a.QuestionId).ToList();

            var questions = await _context.Questions
                .AsNoTracking()
                .Where(q => questionIds.Contains(q.Id))
                .Include(q => q.Answers)
                .Include(q => q.Media)
                .Include(q => q.Group).ThenInclude(g => g.Media)
                // Bỏ ThenInclude(g => g.Questions) → gây cycle với AsNoTracking
                .ToListAsync(cancellationToken);

            var questionDict = questions.ToDictionary(q => q.Id);
            var questionCategoryMap = questions.ToDictionary(q => q.Id, q => q.CategoryId);

            var groupMeta = questions
                .Where(q => q.GroupId.HasValue)
                .GroupBy(q => q.GroupId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(q => q.CreatedAt).ToList());

            // 3. Build session
            var session = new PracticeSessionDto
            {
                SessionId = attempt.Id,
                Title = attempt.Title,
                TotalQuestions = attempt.TotalQuestions,
                Duration = attempt.TimeLimitSeconds.HasValue ? attempt.TimeLimitSeconds.Value / 60 : 0,
                Parts = new List<PracticePartDto>()
            };

            // 4. Chia đúng part theo PartResults đã lưu
            foreach (var partResult in attempt.PartResults.OrderBy(pr => pr.PartNumber))
            {
                var partQuestionIds = questionCategoryMap
                    .Where(kv => kv.Value == partResult.CategoryId)
                    .Select(kv => kv.Key)
                    .ToHashSet();

                var partAnswerData = answerData
                    .Where(a => partQuestionIds.Contains(a.QuestionId))
                    .ToList();

                if (!partAnswerData.Any()) continue;

                var questionDtos = new List<PracticeQuestionDto>();

                foreach (var storedAnswer in partAnswerData)
                {
                    if (!questionDict.TryGetValue(storedAnswer.QuestionId, out var q)) continue;

                    var dto = new PracticeQuestionDto
                    {
                        QuestionId = q.Id,
                        OrderIndex = storedAnswer.OrderIndex,
                        QuestionNumber = storedAnswer.OrderIndex,
                        Content = q.Content,
                        Explanation = q.Explanation,
                        GroupId = q.GroupId,
                        Media = q.Media.OrderBy(m => m.OrderIndex).Select(MapMedia).ToList(),
                        HasAudio = q.Media.Any(m => IsAudio(m.MediaType, m.Url)),
                        HasImage = q.Media.Any(m => IsImage(m.MediaType, m.Url)),
                        AudioUrl = q.Media.FirstOrDefault(m => IsAudio(m.MediaType, m.Url))?.Url,
                        ImageUrl = q.Media.FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url,
                        Answers = q.Answers.OrderBy(a => a.OrderIndex).Select(MapAnswer).ToList(),
                        SelectedAnswerId = storedAnswer.SelectedAnswerId,
                        IsMarkedForReview = storedAnswer.IsMarkedForReview,
                        IsCorrect = null,
                    };

                    if (q.GroupId.HasValue && q.Group != null
                        && groupMeta.TryGetValue(q.GroupId.Value, out var siblings))
                    {
                        dto.GroupContent = q.Group.Content;
                        dto.TotalQuestionsInGroup = siblings.Count;
                        dto.QuestionIndexInGroup = siblings.FindIndex(x => x.Id == q.Id) + 1;
                        dto.GroupMedia = q.Group.Media.OrderBy(m => m.OrderIndex).Select(MapGroupMedia).ToList();
                    }

                    questionDtos.Add(dto);
                }

                session.Parts.Add(new PracticePartDto
                {
                    PartId = partResult.CategoryId,
                    PartName = partResult.PartName,
                    PartNumber = partResult.PartNumber,
                    Questions = questionDtos
                });
            }

            return session;
        }

        private static PracticeMediaDto MapMedia(QuestionMedia m) => new()
        {
            Id = m.Id,
            Url = m.Url,
            Type = ResolveMediaType(m.MediaType, m.Url)
        };

        private static PracticeMediaDto MapGroupMedia(QuestionGroupMedia m) => new()
        {
            Id = m.Id,
            Url = m.Url,
            Type = ResolveMediaType(m.MediaType, m.Url)
        };

        private static PracticeAnswerDto MapAnswer(Answer a) => new()
        {
            Id = a.Id,
            Content = a.Content,
            OrderIndex = a.OrderIndex,
            IsCorrect = a.IsCorrect,
            Media = new List<PracticeMediaDto>()
        };

        private static string ResolveMediaType(string? mediaType, string? url) =>
            !string.IsNullOrWhiteSpace(mediaType) ? mediaType.ToLower() : GetTypeFromUrl(url);

        private static bool IsAudio(string? t, string? u) => ResolveMediaType(t, u) == "audio";
        private static bool IsImage(string? t, string? u) => ResolveMediaType(t, u) == "image";

        private static string GetTypeFromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "unknown";
            var u = url.ToLower();
            if (u.EndsWith(".mp3") || u.EndsWith(".wav") || u.EndsWith(".ogg") || u.EndsWith(".m4a")) return "audio";
            if (u.EndsWith(".jpg") || u.EndsWith(".jpeg") || u.EndsWith(".png") || u.EndsWith(".webp")) return "image";
            if (u.EndsWith(".mp4") || u.EndsWith(".webm")) return "video";
            if (u.Contains("/image/upload/")) return "image";
            return "unknown";
        }
    }
}