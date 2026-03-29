using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Questions.Commands
{
    public class CreateQuestionGroupRequest
    {
        public Guid CategoryId { get; set; }
        public string? GroupContent { get; set; }
        public string? GroupAudioUrl { get; set; }
        public string? GroupImageUrl { get; set; }
        public Guid? DifficultyId { get; set; }
        public string? Explanation { get; set; }
        public string? Transcript { get; set; }
        public string? MediaJson { get; set; }

        // Files
        public IFormFile? GroupAudioFile { get; set; }
        public IFormFile? GroupImageFile { get; set; }

        // Tags dạng array form-data: tags[0]=abc&tags[1]=xyz
        public List<string>? Tags { get; set; }

        // Questions dạng JSON string — dễ nhất cho form-data
        // VD: [{"content":"...","questionType":1,"answers":[...]}]
        public string? QuestionsJson { get; set; }
    }

    public record CreateQuestionGroupCommand : IRequest<Guid>
    {
        public Guid CategoryId { get; set; }
        public string? GroupContent { get; set; }       // Passage / đoạn văn / đề bài
        public string? GroupAudioUrl { get; set; }
        public string? GroupImageUrl { get; set; }
        public Guid? DifficultyId { get; set; }
        public string? Explanation { get; set; }
        public string? Transcript { get; set; }         // Script audio (Listening)
        public string? MediaJson { get; init; }

        // Files upload
        public IFormFile? GroupAudioFile { get; set; }
        public IFormFile? GroupImageFile { get; set; }

        public List<CreateQuestionDto> Questions { get; init; } = [];
        public List<string> Tags { get; init; } = [];
    }

    // Helper dùng trong command handler
    public static class QuestionTypeHelper
    {
        // Các type cần đáp án cố định (có IsCorrect)
        private static readonly HashSet<QuestionTypeEnum> McqTypes = new()
    {
        QuestionTypeEnum.SingleChoice,
        QuestionTypeEnum.MultipleChoice,
        QuestionTypeEnum.TrueFalseNotGiven,
        QuestionTypeEnum.YesNoNotGiven,
        QuestionTypeEnum.MatchingHeading,
        QuestionTypeEnum.MatchingInformation,
        QuestionTypeEnum.Matching,
    };


        public static bool IsMcq(QuestionTypeEnum type) => McqTypes.Contains(type);


        // Số đáp án theo Part + Type
        public static int GetExpectedAnswerCount(string partCode, QuestionTypeEnum type) =>
            (partCode.ToUpper(), type) switch
            {
                ("PART 2", _) => 3,
                (_, QuestionTypeEnum.TrueFalseNotGiven) => 3,  // True/False/Not Given
                (_, QuestionTypeEnum.YesNoNotGiven) => 3,  // Yes/No/Not Given
                _ => 4,
            };

        internal static bool IsFillIn(QuestionTypeEnum questionType)
        {
            throw new NotImplementedException();
        }
    }

    public class CreateQuestionGroupCommandHandler
        : IRequestHandler<CreateQuestionGroupCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;

        // Các code yêu cầu audio
        private static readonly HashSet<string> RequiresAudioCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "PART 3", "PART 4",
            "IELTS_L_S1", "IELTS_L_S2", "IELTS_L_S3", "IELTS_L_S4"
        };

        // Các code yêu cầu passage/content
        private static readonly HashSet<string> RequiresContentCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "PART 6", "PART 7",
            "IELTS_R_P1", "IELTS_R_P2", "IELTS_R_P3"
        };

        // Các code không cần đáp án cố định (AI chấm)
        private static readonly HashSet<string> AiGradedCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "IELTS_W_T1", "IELTS_W_T2",
            "IELTS_SP_P1", "IELTS_SP_P2", "IELTS_SP_P3"
        };

        public CreateQuestionGroupCommandHandler(
            IAppDbContext context,
            ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Guid> Handle(
            CreateQuestionGroupCommand request,
            CancellationToken cancellation)
        {
            var category = await ValidateRequestAsync(request, cancellation);
            await CheckDuplicatesAsync(request, category, cancellation);
            var uploadResults = await UploadAllFilesAsync(request, cancellation);

            using var transaction = await _context.BeginTransactionAsync(cancellation);
            try
            {
                var groupId = Guid.NewGuid();
                var entities = BuildAllEntities(groupId, request, category, uploadResults);

                _context.QuestionGroups.Add(entities.Group);

                if (entities.GroupMedias.Any())
                    _context.QuestionGroupMedia.AddRange(entities.GroupMedias);
                if (entities.Questions.Any())
                    _context.Questions.AddRange(entities.Questions);
                if (entities.Answers.Any())
                    _context.Answers.AddRange(entities.Answers);
                if (entities.QuestionMedias.Any())
                    _context.QuestionMedias.AddRange(entities.QuestionMedias);
                if (entities.Tags.Any())
                    _context.QuestionTags.AddRange(entities.Tags);

                await _context.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);

                return groupId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellation);
                throw;
            }
        }

        #region Validate

        private async Task<Category> ValidateRequestAsync(
            CreateQuestionGroupCommand request,
            CancellationToken cancellation)
        {
            if (request.CategoryId == Guid.Empty)
                throw new ValidationException("Vui lòng chọn danh mục");

            var categoryIds = new List<Guid> { request.CategoryId };
            if (request.DifficultyId.HasValue && request.DifficultyId != Guid.Empty)
                categoryIds.Add(request.DifficultyId.Value);

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellation);

            if (!categories.TryGetValue(request.CategoryId, out var category))
                throw new ValidationException("Danh mục không tồn tại");

            if (request.DifficultyId.HasValue
                && request.DifficultyId != Guid.Empty
                && !categories.ContainsKey(request.DifficultyId.Value))
                throw new ValidationException("Difficulty không tồn tại");

            if (request.Questions == null || !request.Questions.Any())
                throw new ValidationException("Question group phải có ít nhất 1 câu hỏi");

            var code = category.Code.Trim();

            // Kiểm tra audio
            if (RequiresAudioCodes.Contains(code))
            {
                if (request.GroupAudioFile == null && string.IsNullOrWhiteSpace(request.GroupAudioUrl))
                    throw new ValidationException($"{category.Name} phải có Audio");
            }

            // Kiểm tra passage/content
            if (RequiresContentCodes.Contains(code))
            {
                if (string.IsNullOrWhiteSpace(request.GroupContent))
                    throw new ValidationException($"{category.Name} phải có nội dung bài đọc");
            }

            // Validate từng câu hỏi con
            for (int i = 0; i < request.Questions.Count; i++)
                ValidateQuestion(request.Questions[i], i + 1, category);

            return category;
        }

        private void ValidateQuestion(
            CreateQuestionDto dto,
            int index,
            Category partCategory)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ValidationException(
                    $"Câu hỏi {index}: Nội dung không được để trống");

            var type = dto.QuestionType;
            var partCode = partCategory.Code.Trim().ToUpper();
            var isToeic = partCode.StartsWith("PART");

            // ── Completion types (10-16) ──────────────────────────
            // Phải có ít nhất 1 answer IsCorrect=true
            if (IsCompletionType(type))
            {
                if (dto.Answers == null || !dto.Answers.Any())
                    throw new ValidationException(
                        $"Câu hỏi {index}: Completion question phải có ít nhất 1 đáp án đúng");

                var correctCount = dto.Answers.Count(a => a.IsCorrect);
                if (correctCount == 0)
                    throw new ValidationException(
                        $"Câu hỏi {index}: Phải có ít nhất 1 đáp án đúng " +
                        $"(IsCorrect=true) để chấm điểm");

                if (dto.Answers.Any(a => string.IsNullOrWhiteSpace(a.Content)))
                    throw new ValidationException(
                        $"Câu hỏi {index}: Đáp án không được để trống");

                return;
            }

            // ── MCQ / Matching / T-F-NG (1-9) ────────────────────
            if (IsMcqType(type))
            {
                if (dto.Answers == null || dto.Answers.Count < 2)
                    throw new ValidationException(
                        $"Câu hỏi {index}: Phải có ít nhất 2 đáp án");

                // TOEIC validate cứng số đáp án
                if (isToeic)
                {
                    var expected = GetExpectedAnswerCount(partCode, type);
                    if (dto.Answers.Count != expected)
                        throw new ValidationException(
                            $"Câu hỏi {index}: {partCategory.Name} phải có đúng {expected} đáp án");
                }

                var correctCount = dto.Answers.Count(a => a.IsCorrect);
                if (type == QuestionTypeEnum.MultipleChoice)
                {
                    if (correctCount < 2)
                        throw new ValidationException(
                            $"Câu hỏi {index}: Multiple Choice phải có ít nhất 2 đáp án đúng");
                }
                else
                {
                    if (correctCount != 1)
                        throw new ValidationException(
                            $"Câu hỏi {index}: Phải có đúng 1 đáp án đúng " +
                            $"(Tìm thấy {correctCount})");
                }

                var dupOrders = dto.Answers
                    .GroupBy(a => a.OrderIndex)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                if (dupOrders.Any())
                    throw new ValidationException(
                        $"Câu hỏi {index}: OrderIndex bị trùng: " +
                        $"{string.Join(", ", dupOrders)}");
            }
        }

        // ── Type helpers ──────────────────────────────────────────
        private static bool IsCompletionType(QuestionTypeEnum t) =>
            t is QuestionTypeEnum.ShortAnswer
              or QuestionTypeEnum.NoteCompletion
              or QuestionTypeEnum.FormCompletion
              or QuestionTypeEnum.TableCompletion
              or QuestionTypeEnum.SummaryCompletion
              or QuestionTypeEnum.SentenceCompletion
              or QuestionTypeEnum.MapLabeling;

        private static bool IsMcqType(QuestionTypeEnum t) =>
            t is QuestionTypeEnum.SingleChoice
              or QuestionTypeEnum.MultipleChoice
              or QuestionTypeEnum.FillBlank
              or QuestionTypeEnum.Matching
              or QuestionTypeEnum.MatchingHeading
              or QuestionTypeEnum.MatchingInformation
              or QuestionTypeEnum.MatchingSentenceEnds
              or QuestionTypeEnum.TrueFalseNotGiven
              or QuestionTypeEnum.YesNoNotGiven;

        // Số đáp án theo từng loại Part
        private static int GetExpectedAnswerCount(string partCode, QuestionTypeEnum type) =>
     (partCode.ToUpper(), type) switch
     {
         // TOEIC Part 2: 3 đáp án
         ("PART 2", _) => 3,

         // True/False/Not Given: 3 đáp án
         (_, QuestionTypeEnum.TrueFalseNotGiven) => 3,
         (_, QuestionTypeEnum.YesNoNotGiven) => 3,

         // Tất cả còn lại: 4 đáp án
         _ => 4,
     };

        #endregion

        #region Duplicate Check

        private async Task CheckDuplicatesAsync(
            CreateQuestionGroupCommand request,
            Category category,
            CancellationToken cancellation)
        {
            var errors = new List<string>();
            var code = category.Code.Trim();

            // Bỏ qua check duplicate cho AI graded (Writing/Speaking)
            if (AiGradedCodes.Contains(code)) return;

            // 1. Group content duplicate
            if (!string.IsNullOrWhiteSpace(request.GroupContent))
            {
                var dup = await CheckGroupContentDuplicateAsync(
                    request.CategoryId, request.GroupContent, cancellation);
                if (dup != null)
                    errors.Add($"Nội dung nhóm trùng {dup.Similarity:P0} với nhóm ID: {dup.GroupId}");
            }

            // 2. Question content duplicates
            for (int i = 0; i < request.Questions.Count; i++)
            {
                var q = request.Questions[i];
                if (!string.IsNullOrWhiteSpace(q.Content))
                {
                    var dup = await CheckQuestionContentDuplicateAsync(
                        request.CategoryId, q.Content, cancellation);
                    if (dup != null)
                        errors.Add($"Câu hỏi {i + 1}: Nội dung trùng {dup.Similarity:P0} " +
                                   $"với câu hỏi ID: {dup.QuestionId}");
                }
            }

            // 3. Audio file hash
            if (request.GroupAudioFile != null)
            {
                var hash = await CalculateFileHashAsync(request.GroupAudioFile);
                var exists = await _context.QuestionGroupMedia
                    .AnyAsync(m => m.FileHash == hash && m.MediaType == "audio", cancellation);
                if (exists)
                    errors.Add("File audio nhóm đã tồn tại trong hệ thống");
            }

            // 4. Image file hash
            if (request.GroupImageFile != null)
            {
                var hash = await CalculateFileHashAsync(request.GroupImageFile);
                var exists = await _context.QuestionGroupMedia
                    .AnyAsync(m => m.FileHash == hash && m.MediaType == "image", cancellation);
                if (exists)
                    errors.Add("File ảnh nhóm đã tồn tại trong hệ thống");
            }

            if (errors.Any())
                throw new ValidationException(
                    $"Phát hiện trùng lặp:\n• {string.Join("\n• ", errors)}");
        }

        private async Task<DuplicateInfo?> CheckGroupContentDuplicateAsync(
            Guid categoryId, string content, CancellationToken cancellation)
        {
            var clean = StripHtml(content).Trim().ToLower();
            if (clean.Length < 50) return null;

            var existing = await _context.QuestionGroups
                .Where(g => g.CategoryId == categoryId && g.IsActive)
                .OrderByDescending(g => g.CreatedAt)
                .Take(200)
                .Select(g => new { g.Id, g.Content })
                .ToListAsync(cancellation);

            foreach (var item in existing)
            {
                var existClean = StripHtml(item.Content ?? "").Trim().ToLower();
                if (Math.Abs(clean.Length - existClean.Length) > clean.Length * 0.3) continue;
                var sim = CalculateSimilarity(clean, existClean);
                if (sim > 0.85)
                    return new DuplicateInfo { GroupId = item.Id, Similarity = sim };
            }
            return null;
        }

        private async Task<DuplicateInfo?> CheckQuestionContentDuplicateAsync(
            Guid categoryId, string content, CancellationToken cancellation)
        {
            var clean = StripHtml(content).Trim().ToLower();
            if (clean.Length < 10) return null;

            var existing = await _context.Questions
                .Where(q => q.CategoryId == categoryId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .Take(300)
                .Select(q => new { q.Id, q.Content })
                .ToListAsync(cancellation);

            foreach (var item in existing)
            {
                var existClean = StripHtml(item.Content ?? "").Trim().ToLower();
                if (Math.Abs(clean.Length - existClean.Length) > clean.Length * 0.2) continue;
                var sim = CalculateSimilarity(clean, existClean);
                if (sim > 0.85)
                    return new DuplicateInfo { QuestionId = item.Id, Similarity = sim };
            }
            return null;
        }

        #endregion

        #region Upload

        private class UploadResults
        {
            public string? GroupAudioUrl { get; set; }
            public string? GroupAudioPublicId { get; set; }
            public string? GroupImageUrl { get; set; }
            public string? GroupImagePublicId { get; set; }
            public string? GroupAudioFileHash { get; set; }
            public string? GroupImageFileHash { get; set; }
        }

        private async Task<UploadResults> UploadAllFilesAsync(
            CreateQuestionGroupCommand request,
            CancellationToken cancellation)
        {
            var results = new UploadResults();
            var tasks = new List<Task>();

            // Xác định folder upload theo loại bài thi
            var code = string.Empty;
            var audioFolder = "questions/groups/audio";
            var imageFolder = "questions/groups/images";

            if (request.GroupAudioFile != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    results.GroupAudioFileHash = await CalculateFileHashAsync(request.GroupAudioFile);
                    var r = await _cloudinary.UploadAudioAsync(
                        request.GroupAudioFile, audioFolder, cancellation);
                    results.GroupAudioUrl = r.Url;
                    results.GroupAudioPublicId = r.PublicId;
                }, cancellation));
            }
            else if (!string.IsNullOrWhiteSpace(request.GroupAudioUrl))
            {
                results.GroupAudioUrl = request.GroupAudioUrl;
            }

            if (request.GroupImageFile != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    results.GroupImageFileHash = await CalculateFileHashAsync(request.GroupImageFile);
                    var r = await _cloudinary.UploadImageAsync(
                        request.GroupImageFile, imageFolder, cancellation);
                    results.GroupImageUrl = r.Url;
                    results.GroupImagePublicId = r.PublicId;
                }, cancellation));
            }
            else if (!string.IsNullOrWhiteSpace(request.GroupImageUrl))
            {
                results.GroupImageUrl = request.GroupImageUrl;
            }

            await Task.WhenAll(tasks);
            return results;
        }

        #endregion

        #region Build Entities

        private (
            QuestionGroup Group,
            List<QuestionGroupMedia> GroupMedias,
            List<Question> Questions,
            List<Answer> Answers,
            List<QuestionMedia> QuestionMedias,
            List<QuestionTag> Tags)
        BuildAllEntities(
            Guid groupId,
            CreateQuestionGroupCommand request,
            Category category,
            UploadResults upload)
        {
            var now = DateTime.UtcNow;
            var code = category.Code.Trim();
            var isAiGraded = AiGradedCodes.Contains(code);

            // Group
            var group = new QuestionGroup
            {
                Id = groupId,
                CategoryId = request.CategoryId,
                Content = request.GroupContent,
                Explanation = request.Explanation,
                DifficultyId = request.DifficultyId,
                Transcript = request.Transcript,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // Group Medias
            var groupMedias = new List<QuestionGroupMedia>();
            int mediaIdx = 1;

            if (!string.IsNullOrWhiteSpace(upload.GroupAudioUrl))
                groupMedias.Add(new QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = upload.GroupAudioUrl,
                    PublicId = upload.GroupAudioPublicId ?? string.Empty,
                    MediaType = "audio",
                    OrderIndex = mediaIdx++,
                    FileHash = upload.GroupAudioFileHash,
                });

            if (!string.IsNullOrWhiteSpace(upload.GroupImageUrl))
                groupMedias.Add(new QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = upload.GroupImageUrl,
                    PublicId = upload.GroupImagePublicId ?? string.Empty,
                    MediaType = "image",
                    OrderIndex = mediaIdx++,
                    FileHash = upload.GroupImageFileHash,
                });

            // Questions + Answers + Medias
            // Questions + Answers + Medias
            var questions = new List<Question>();
            var allAnswers = new List<Answer>();
            var allQMedias = new List<QuestionMedia>();

            foreach (var (dto, idx) in request.Questions.Select((q, i) => (q, i + 1)))
            {
                var questionId = Guid.NewGuid();
                var questionType = dto.QuestionType == default
                    ? QuestionTypeEnum.SingleChoice
                    : dto.QuestionType;

                var question = new Question
                {
                    Id = questionId,
                    GroupId = groupId,
                    Content = dto.Content,
                    CategoryId = request.CategoryId,
                    QuestionType = questionType,
                    DifficultyId = request.DifficultyId,
                    Explanation = dto.Explanation,
                    DefaultScore = dto.DefaultScore,
                    ShuffleAnswers = dto.ShuffleAnswers,
                    MaxWords = dto.MaxWords,
                    IsActive = true,
                    OrderIndex = idx,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                questions.Add(question);

                if (dto.Answers?.Any() == true)
                {
                    allAnswers.AddRange(dto.Answers.Select((a, aIdx) => new Answer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        Content = a.Content?.Trim() ?? string.Empty,
                        IsCorrect = a.IsCorrect,
                        Feedback = a.Feedback,
                        OrderIndex = a.OrderIndex > 0 ? a.OrderIndex : aIdx + 1,
                        CreatedAt = now,
                        UpdatedAt = now,
                    }));
                }

                // Question Medias
                if (dto.Media?.Any() == true)
                {
                    allQMedias.AddRange(dto.Media.Select(m => new QuestionMedia
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        Url = m.Url,
                        MediaType = m.MediaType,
                        OrderIndex = m.OrderIndex,
                        CreatedAt = now,
                        UpdatedAt = now,
                    }));
                }
            }

            // Tags
            var tags = request.Tags?
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .Select(t => new QuestionTag
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Tag = t.Trim(),
                    TagType = "Topic",
                    CreatedAt = now,
                }).ToList() ?? new List<QuestionTag>();

            return (group, groupMedias, questions, allAnswers, allQMedias, tags);
        }

        #endregion

        #region Helpers

        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;
            if (s1 == s2) return 1.0;
            var dist = LevenshteinDistance(s1, s2);
            return 1.0 - (double)dist / Math.Max(s1.Length, s2.Length);
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            var matrix = new int[s1.Length + 1, s2.Length + 1];
            for (int i = 0; i <= s1.Length; i++) matrix[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++) matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }

            return matrix[s1.Length, s2.Length];
        }

        private async Task<string> CalculateFileHashAsync(IFormFile file)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = file.OpenReadStream();
            var hash = await sha256.ComputeHashAsync(stream);
            stream.Position = 0;
            return Convert.ToBase64String(hash);
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }

        private class DuplicateInfo
        {
            public Guid? GroupId { get; set; }
            public Guid? QuestionId { get; set; }
            public double Similarity { get; set; }
        }

        #endregion
    }
}