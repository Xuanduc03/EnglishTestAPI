using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Practices.Queries
{
    public record GetPracticeByPartQuery(
        List<Guid> CategoryIds,
        int QuestionsPerPart = 10,
        bool RandomOrder = true
    ) : IRequest<PracticeSessionDto>;

    public class GetPracticeByPartQueryHandler : IRequestHandler<GetPracticeByPartQuery, PracticeSessionDto>
    {
        private readonly IAppDbContext _context;

        // FIX 1: Bỏ IMapper
        public GetPracticeByPartQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PracticeSessionDto> Handle(GetPracticeByPartQuery request, CancellationToken cancellationToken)
        {
            var session = new PracticeSessionDto
            {
                SessionId = Guid.NewGuid(),
                Title = "Practice Session",
                TotalQuestions = 0,
                Parts = new List<PracticePartDto>()
            };

            int globalQuestionNumber = 1;

            foreach (var categoryId in request.CategoryIds)
            {
                var category = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

                if (category == null) continue;

                var questions = await GetQuestionsByPart(
                    categoryId, request.QuestionsPerPart, request.RandomOrder, cancellationToken);

                // FIX 2: Gọi MapQuestions thay vì ProcessQuestions (không tồn tại)
                var questionDtos = MapQuestions(questions, globalQuestionNumber);

                session.Parts.Add(new PracticePartDto
                {
                    PartId = category.Id,
                    PartName = category.Name,
                    PartNumber = ExtractPartNumber(category.Name),
                    PartDescription = category.Description,
                    Questions = questionDtos
                });

                globalQuestionNumber += questionDtos.Count;
                session.TotalQuestions += questionDtos.Count;
            }

            session.Duration = CalculateDuration(session.Parts);

            return session;
        }

        private async Task<List<Question>> GetQuestionsByPart(
            Guid categoryId, int count, bool randomOrder, CancellationToken cancellationToken)
        {
            var all = await _context.Questions
                .AsNoTracking()
                .Where(q => q.CategoryId == categoryId && !q.IsDeleted)
                .Include(q => q.Media)
                .Include(q => q.Answers)
                .Include(q => q.Group).ThenInclude(g => g.Media)
                .ToListAsync(cancellationToken);

            var singles = all.Where(q => q.GroupId == null).ToList();
            var grouped = all.Where(q => q.GroupId != null).ToList();

            if (!grouped.Any())
            {
                return randomOrder
                    ? singles.OrderBy(_ => Guid.NewGuid()).Take(count).ToList()
                    : singles.Take(count).ToList();
            }

            var groupIds = grouped.Select(q => q.GroupId!.Value).Distinct().ToList();
            if (randomOrder) groupIds = groupIds.OrderBy(_ => Guid.NewGuid()).ToList();

            int groupsNeeded = (int)Math.Ceiling((double)count / 3.0);
            var selectedGroupIds = groupIds.Take(groupsNeeded).ToHashSet();

            return grouped
                .Where(q => selectedGroupIds.Contains(q.GroupId!.Value))
                .OrderBy(q => q.GroupId)
                .ThenBy(q => q.Id)
                .Take(count)
                .ToList();
        }

        private static List<PracticeQuestionDto> MapQuestions(List<Question> questions, int startNumber)
        {
            var result = new List<PracticeQuestionDto>();
            var orderIndex = 1;

            var groupMeta = questions
                .Where(q => q.GroupId.HasValue)
                .GroupBy(q => q.GroupId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(q => q.CreatedAt).ToList());

            foreach (var q in questions)
            {
                var dto = new PracticeQuestionDto
                {
                    QuestionId = q.Id,
                    OrderIndex = orderIndex,
                    QuestionNumber = startNumber + orderIndex - 1,
                    Content = q.Content,
                    Explanation = q.Explanation,
                    GroupId = q.GroupId,
                    Media = q.Media.OrderBy(m => m.OrderIndex).Select(MapMedia).ToList(),
                    HasAudio = q.Media.Any(m => IsAudio(m.MediaType, m.Url)),
                    HasImage = q.Media.Any(m => IsImage(m.MediaType, m.Url)),
                    AudioUrl = q.Media.FirstOrDefault(m => IsAudio(m.MediaType, m.Url))?.Url,
                    ImageUrl = q.Media.FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url,
                    Answers = q.Answers.OrderBy(a => a.OrderIndex).Select(MapAnswer).ToList(),
                    SelectedAnswerId = null,
                    IsCorrect = null,
                    IsMarkedForReview = false,
                };

                if (q.GroupId.HasValue && q.Group != null && groupMeta.TryGetValue(q.GroupId.Value, out var siblings))
                {
                    dto.GroupContent = q.Group.Content;
                    dto.TotalQuestionsInGroup = siblings.Count;
                    dto.QuestionIndexInGroup = siblings.FindIndex(x => x.Id == q.Id) + 1;
                    dto.GroupMedia = q.Group.Media.OrderBy(m => m.OrderIndex).Select(MapGroupMedia).ToList();
                }

                result.Add(dto);
                orderIndex++;
            }

            return result;
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

        // FIX 4: Bỏ ExtractPartNumberFromId (sync DB call — xấu), chỉ giữ static version
        private static int ExtractPartNumber(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return 0;
            var parts = partName.Split(' ');
            return parts.Length >= 2 && int.TryParse(parts[1], out int n) ? n : 0;
        }

        private static int CalculateDuration(List<PracticePartDto> parts)
        {
            var t = new Dictionary<int, double> { { 1, .5 }, { 2, .5 }, { 3, 1.5 }, { 4, 1.5 }, { 5, .5 }, { 6, 1.0 }, { 7, 1.5 } };
            return parts.Sum(p => t.TryGetValue(p.PartNumber, out var v) ? (int)(p.Questions.Count * v) : p.Questions.Count);
        }
    }
}