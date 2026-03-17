using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Writing.Queries
{
    // ============================================================
    // GET WRITING QUESTIONS QUERY
    // Lấy câu hỏi cho Writing session theo danh sách CategoryId
    // Mỗi CategoryId tương ứng 1 Writing Part (1, 2, hoặc 3)
    // ============================================================

    public record GetWritingQuestionsQuery(
        List<Guid> CategoryIds,
        bool RandomOrder = true
    ) : IRequest<WritingSessionDto>;

    public class GetWritingQuestionsQueryHandler
        : IRequestHandler<GetWritingQuestionsQuery, WritingSessionDto>
    {
        private readonly IAppDbContext _context;

        // Cấu hình mặc định cho từng Writing Part
        private static readonly Dictionary<int, WritingPartConfig> PartConfigs = new()
        {
            [1] = new("Writing Part 1", WritingPartType.SentenceFromPhoto,
                      "Write ONE sentence using the TWO words or phrases given.",
                      QuestionsCount: 8, TimeLimitSeconds: 480), // 1 phút/câu
            [2] = new("Writing Part 2", WritingPartType.EmailResponse,
                      "Read the email and respond. Address all THREE points mentioned.",
                      QuestionsCount: 2, TimeLimitSeconds: 1200), // 10 phút/câu
            [3] = new("Writing Part 3", WritingPartType.OpinionEssay,
                      "Give your opinion on the following topic. Write at least 300 words.",
                      QuestionsCount: 1, TimeLimitSeconds: 1200)  // 20 phút
        };

        public GetWritingQuestionsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<WritingSessionDto> Handle(
            GetWritingQuestionsQuery request,
            CancellationToken cancellationToken)
        {
            var session = new WritingSessionDto
            {
                SessionId = Guid.NewGuid(),
                Parts = new List<WritingPartDto>()
            };

            int globalQuestionNumber = 1;
            int totalTimeSeconds = 0;

            foreach (var categoryId in request.CategoryIds)
            {
                var category = await _context.Categories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

                if (category == null) continue;

                int partNumber = ExtractPartNumber(category.Name);
                if (!PartConfigs.TryGetValue(partNumber, out var config)) continue;

                // Lấy câu hỏi Writing từ DB
                var dbQuestions = await _context.Questions
                    .AsNoTracking()
                    .Where(q => q.CategoryId == categoryId
                             && !q.IsDeleted
                             && q.QuestionType == QuestionTypeEnum.Writing)
                    .Include(q => q.Media)
                    .ToListAsync(cancellationToken);

                // Random hoặc lấy theo thứ tự, giới hạn số lượng
                var selected = request.RandomOrder
                    ? dbQuestions.OrderBy(_ => Guid.NewGuid()).Take(config.QuestionsCount).ToList()
                    : dbQuestions.Take(config.QuestionsCount).ToList();

                var questionDtos = MapWritingQuestions(selected, config.PartType, globalQuestionNumber);

                session.Parts.Add(new WritingPartDto
                {
                    PartId = category.Id,
                    PartName = config.Name,
                    PartNumber = partNumber,
                    Description = category.Description ?? string.Empty,
                    Instructions = config.Instructions,
                    TimeLimitSeconds = config.TimeLimitSeconds,
                    Questions = questionDtos
                });

                globalQuestionNumber += questionDtos.Count;
                session.TotalQuestions += questionDtos.Count;
                totalTimeSeconds += config.TimeLimitSeconds;
            }

            session.TimeLimitSeconds = totalTimeSeconds;
            session.Title = BuildTitle(session.Parts);

            return session;
        }

        // ── Mapping ──────────────────────────────────────────────────

        private static List<WritingQuestionDto> MapWritingQuestions(
            List<Question> questions,
            WritingPartType partType,
            int startNumber)
        {
            var result = new List<WritingQuestionDto>();
            int orderIndex = 1;

            foreach (var q in questions)
            {
                var dto = new WritingQuestionDto
                {
                    QuestionId = q.Id,
                    OrderIndex = orderIndex,
                    QuestionNumber = startNumber + orderIndex - 1,
                    PartType = partType,
                    Content = q.Content,
                    AnswerStatus = WritingAnswerStatus.NotStarted
                };

                switch (partType)
                {
                    case WritingPartType.SentenceFromPhoto:
                        dto.ImageUrl = q.Media
                            .FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url;
                        // RequiredWords được lưu trong Content dạng "word1|word2"
                        // hoặc trong RubricJson — parse theo convention của hệ thống
                        dto.RequiredWords = ParseRequiredWords(q.RubricJson);
                        break;

                    case WritingPartType.EmailResponse:
                        dto.EmailContent = q.Content;
                        dto.ResponsePoints = ParseResponsePoints(q.RubricJson);
                        break;

                    case WritingPartType.OpinionEssay:
                        dto.EssayPrompt = q.Content;
                        dto.MinWords = 300;
                        break;
                }

                result.Add(dto);
                orderIndex++;
            }

            return result;
        }

        // ── Helpers ──────────────────────────────────────────────────

        /// <summary>
        /// RubricJson cho Part 1 có dạng:
        /// { "requiredWords": ["photograph", "taken"] }
        /// </summary>
        private static List<string> ParseRequiredWords(string? rubricJson)
        {
            if (string.IsNullOrWhiteSpace(rubricJson)) return new();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rubricJson);
                if (doc.RootElement.TryGetProperty("requiredWords", out var arr))
                    return arr.EnumerateArray()
                              .Select(e => e.GetString() ?? string.Empty)
                              .Where(s => !string.IsNullOrEmpty(s))
                              .ToList();
            }
            catch { /* ignore malformed JSON */ }
            return new();
        }

        /// <summary>
        /// RubricJson cho Part 2 có dạng:
        /// { "responsePoints": ["Accept or decline", "Suggest another date", "Explain reason"] }
        /// </summary>
        private static List<string> ParseResponsePoints(string? rubricJson)
        {
            if (string.IsNullOrWhiteSpace(rubricJson)) return new();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(rubricJson);
                if (doc.RootElement.TryGetProperty("responsePoints", out var arr))
                    return arr.EnumerateArray()
                              .Select(e => e.GetString() ?? string.Empty)
                              .Where(s => !string.IsNullOrEmpty(s))
                              .ToList();
            }
            catch { }
            return new();
        }

        private static bool IsImage(string? mediaType, string? url)
        {
            if (!string.IsNullOrWhiteSpace(mediaType))
                return mediaType.ToLower() == "image";
            if (string.IsNullOrEmpty(url)) return false;
            var u = url.ToLower();
            return u.EndsWith(".jpg") || u.EndsWith(".jpeg")
                || u.EndsWith(".png") || u.EndsWith(".webp")
                || u.Contains("/image/upload/");
        }

        private static int ExtractPartNumber(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;

            // Đưa tất cả về chữ thường cho dễ xử lý
            var normalizedName = name.ToLowerInvariant();

            // Bắt cả "part 1" và "task 1"
            if (normalizedName.Contains("part 1") || normalizedName.Contains("task 1")) return 1;
            if (normalizedName.Contains("part 2") || normalizedName.Contains("task 2")) return 2;
            if (normalizedName.Contains("part 3") || normalizedName.Contains("task 3")) return 3;

            return 0; // Trả về 0 nếu không tìm thấy
        }

        private static string BuildTitle(List<WritingPartDto> parts)
        {
            if (parts.Count == 1) return $"{parts[0].PartName} Practice";
            var labels = parts.Select(p => $"Part {p.PartNumber}");
            return $"Writing Practice ({string.Join(", ", labels)})";
        }
    }

    // ── Internal config record ────────────────────────────────────

    internal record WritingPartConfig(
       string Name,
       WritingPartType PartType,
       string Instructions,
       int QuestionsCount,
       int TimeLimitSeconds);
}