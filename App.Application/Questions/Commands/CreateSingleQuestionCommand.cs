using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using App.Domain.Entities;
using CloudinaryDotNet.Actions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace App.Application.Questions.Commands
{
    public record CreateSingleQuestionCommand : IRequest<Guid>
    {
        public Guid CategoryId { get; init; }
        public string? Content { get; init; } = string.Empty;
        public string QuestionType { get; init; } = "SingleChoice";
        public Guid? DifficultyId { get; init; }
        public double DefaultScore { get; init; } = 1.0;
        public bool ShuffleAnswers { get; init; } = true;
        public string? Explanation { get; init; }

        // FILES - Upload trực tiếp
        public IFormFile? AudioFile { get; init; }
        public string? AudioUrl { get; init; }
        public IFormFile? ImageFile { get; init; }
        public string? ImageUrl { get; init; }

        // Danh sách đáp án (có thể có audio riêng)
        public List<CreateAnswerWithFileDto> Answers { get; init; } = [];

        // Tags
        public List<string> Tags { get; init; } = [];
    }

    public class CreateAnswerWithFileDto
    {
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; }
        public int OrderIndex { get; set; }

        // File audio cho đáp án (Part 1)
        public IFormFile? AudioFile { get; set; }
    }

    public class CreateSingleQuestionCommandHandler
        : IRequestHandler<CreateSingleQuestionCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        private const int TOEIC_ANSWER_COUNT = 4;

        public CreateSingleQuestionCommandHandler(
            IAppDbContext context,
            ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Guid> Handle(
            CreateSingleQuestionCommand request,
            CancellationToken cancellationToken)
        {

            // 1️⃣ VALIDATION
            var category = await ValidateRequestAsync(request, cancellationToken);

            await CheckDuplicatesAsync(request, category, cancellationToken);

            var uploadResults = await UploadAllFilesAsync(request, cancellationToken);

            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {

                var questionId = Guid.NewGuid();

                var entitiesToAdd = BuildAllEntities(
                    questionId, request, category, uploadResults);

                // single batch insert
                _context.Questions.Add(entitiesToAdd.Question);

                if (entitiesToAdd.Medias.Any())
                {
                    _context.QuestionMedias.AddRange(entitiesToAdd.Medias);
                }

                if (entitiesToAdd.Answer.Any())
                {
                    _context.Answers.AddRange(entitiesToAdd.Answer);
                }

                if (entitiesToAdd.Tags.Any())
                {
                    _context.QuestionTags.AddRange(entitiesToAdd.Tags);
                }
                // 7️⃣ SAVE
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return questionId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        #region ->  1 Validate method

        private async Task<Category> ValidateRequestAsync(
            CreateSingleQuestionCommand request,
            CancellationToken cancellationToken)
        {
            if (request.CategoryId == Guid.Empty)
                throw new ValidationException("Vui lòng chọn danh mục");

            var categoryIds = new List<Guid> { request.CategoryId };

            // 2. ✅ Check DifficultyId
            if (request.DifficultyId.HasValue && request.DifficultyId.Value != Guid.Empty)
            {
                categoryIds.Add(request.DifficultyId.Value);
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c, cancellationToken);

            if (!categories.TryGetValue(request.CategoryId, out var category))
                throw new Exception("Danh mục không tồn tại");

            if (request.DifficultyId.HasValue
           && request.DifficultyId.Value != Guid.Empty
           && !categories.ContainsKey(request.DifficultyId.Value))
            {
                throw new ValidationException("Difficulty không tồn tại");
            }


            if (request.Answers == null || !request.Answers.Any())
                throw new ValidationException("Câu hỏi phải có ít nhất 1 đáp án");

            // TOEIC answer count validation
            var expectedAnswerCount = category.Name.Contains("Part 2") ? 3 : TOEIC_ANSWER_COUNT;
            if (request.Answers.Count != expectedAnswerCount)
                throw new ValidationException(
                    $"{category.Name} phải có chính xác {expectedAnswerCount} đáp án");

            var correctCount = request.Answers.Count(a => a.IsCorrect);
            if (correctCount != 1)
                throw new ValidationException(
                    $"Câu hỏi phải có ít nhất 1 đáp án đúng (Tìm thấy {correctCount})");

            // Part-specific validations
            if (category.Name.Contains("Part 1") || category.Name.Contains("Part 2"))
            {
                if (request.AudioFile == null && request.AudioUrl == null)
                    throw new ValidationException($"{category.Name} phải có Audio");
            }

            if (category.Name.Contains("Part 1"))
            {
                if (request.ImageFile == null)
                    throw new ValidationException("Part 1 vui lòng thêm ít nhất 1 ảnh");
            }

            return category;
        }

        #endregion

        #region -> 2check duplicate flow questions content and answer
        private async Task CheckDuplicatesAsync(CreateSingleQuestionCommand request, Category category, CancellationToken cancellation)
        {
            var errors = new List<string>();

            //1 check content duplicate 
            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                var duplicate = await CheckContentDuplicateAsync(
                    request.CategoryId,
                    request.Content,
                    cancellation
                );
                if (duplicate != null)
                {
                    errors.Add(
                        $"Nội dung câu hỏi trùng {duplicate.Similarity:P0} với câu hỏi ID: {duplicate.QuestionId}");
                }
            }

            // 2️⃣ Answer set duplicate check
            var answerDuplicate = await CheckAnswerSetDuplicateAsync(
                   request.CategoryId,
                   request.Answers,
                   cancellation
               );
            if (answerDuplicate != null)
            {
                errors.Add($"Bộ đáp án giống hệt câu hỏi ID: {answerDuplicate.QuestionId}");
            }


            // 3️⃣ Audio file hash check (if file exists)
            if (request.AudioFile != null)
            {
                var audioHash = await CalculateFileHashAsync(request.AudioFile);
                var exists = await _context.QuestionMedias
                    .Where(m => m.FileHash == audioHash && m.MediaType == "audio")
                    .AnyAsync(cancellation);

                if (exists)
                {
                    errors.Add("File audio đã tồn tại trong hệ thống");
                }
            }

            // 4️⃣ Image file hash check (if file exists)
            if (request.ImageFile != null)
            {
                var imageHash = await CalculateFileHashAsync(request.ImageFile);

                var exists = await _context.QuestionMedias
                    .Where(m => m.FileHash == imageHash && m.MediaType == "image")
                    .AnyAsync(cancellation);

                if (exists)
                {
                    errors.Add("File ảnh đã tồn tại trong hệ thống");

                }
            }
            // Throw if any duplicates found
            if (errors.Any())
            {
                throw new ValidationException(
                    $"⚠️ Phát hiện trùng lặp:\n• {string.Join("\n• ", errors)}"
                );
            }
        }

        //check content similarity
        private async Task<DuplicateInfo?> CheckContentDuplicateAsync(Guid CategoryId, string content, CancellationToken cancellation)
        {
            var cleanContent = StripHtml(content).Trim().ToLower();

            if (cleanContent.Length < 10) return null;

            // only fetch id and content 
            var existingQuestions = await _context.Questions
                .Where(q => q.CategoryId == CategoryId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .Take(300)
                .Select(q => new { q.Id, q.Content })
                .ToListAsync(cancellation);

            // ✅ OPTIMIZE: Check in memory to avoid multiple DB hits
            foreach (var exist in existingQuestions)
            {
                var existingClean = StripHtml(exist.Content ?? "").Trim().ToLower();

                // ✅ Quick length check before expensive calculation
                if (Math.Abs(cleanContent.Length - existingClean.Length) > cleanContent.Length * 0.2)
                    continue;

                var similarity = CalculateSimilarity(cleanContent, existingClean);

                if (similarity > 0.85)
                {
                    return new DuplicateInfo
                    {
                        QuestionId = exist.Id,
                        Similarity = similarity
                    };

                }
            }

            return null;
        }

        // check trùng đáp án 
        private async Task<DuplicateInfo?> CheckAnswerSetDuplicateAsync(Guid categoryId, List<CreateAnswerWithFileDto> newAnswers, CancellationToken cancellation)
        {
            var answerSignature = CreateAnswerSetSignature(newAnswers);

            // include answer
            var recentQuestion = await _context.Questions
                .Where(q => q.CategoryId == categoryId && q.IsActive)
                .Include(a => a.Answers)
                .AsSplitQuery()
                .OrderByDescending(q => q.CreatedAt)
                .Take(200)
                .ToListAsync(cancellation);

            foreach (var existing in recentQuestion)
            {
                if (existing.Answers?.Any() != true) continue;

                var existingSignature = CreateAnswerSetSignature(existing.Answers);

                if (answerSignature == existingSignature)
                {
                    return new DuplicateInfo
                    {
                        QuestionId = existing.Id,
                        Similarity = 1.0
                    };
                }
            }

            return null;
        }
        #endregion


        #region // ========== CREATE MEDIA AND AUDIO TO CLOUDINARY ==========

        // ✅ Upload result model
        private class UploadResults
        {
            public string? QuestionAudioUrl { get; set; }
            public string? QuestionAudioPublicId { get; set; }
            public string? QuestionImageUrl { get; set; }
            public string? QuestionImagePublicId { get; set; }
            public Dictionary<int, (string Url, string PublicId)> AnswerAudios { get; set; } = new();

            // ✅ Store hashes for deduplication
            public string? AudioFileHash { get; set; }
            public string? ImageFileHash { get; set; }
        }
        private async Task<UploadResults> UploadAllFilesAsync(
        CreateSingleQuestionCommand request,
        CancellationToken cancellationToken)
        {
            var results = new UploadResults();
            var uploadTasks = new List<Task>();


            if (request.AudioFile != null)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    results.AudioFileHash = await CalculateFileHashAsync(request.AudioFile);
                    var audioResult = await _cloudinary.UploadAudioAsync(
                        request.AudioFile,
                        "toeic/questions/audio",
                        cancellationToken
                    );
                    results.QuestionAudioUrl = audioResult.Url;
                    results.QuestionAudioPublicId = audioResult.PublicId;
                }, cancellationToken));
            }
            else if (!string.IsNullOrWhiteSpace(request.AudioUrl))
            {
                results.QuestionAudioUrl = request.AudioUrl;
            }

            // ========== IMAGE ==========

            if (request.ImageFile != null)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    results.ImageFileHash = await CalculateFileHashAsync(request.ImageFile);
                    var imageResult = await _cloudinary.UploadImageAsync(
                        request.ImageFile,
                        "toeic/questions/images",
                        cancellationToken
                    );
                    results.QuestionImageUrl = imageResult.Url;
                    results.QuestionImagePublicId = imageResult.PublicId;
                }, cancellationToken));
            }
            else if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                results.QuestionImageUrl = request.ImageUrl;
            }

            await Task.WhenAll(uploadTasks);

            return results;
        }
        #endregion

        #region Build entity

        private (Question Question, List<QuestionMedia> Medias, List<Answer> Answer, List<QuestionTag> Tags)
            BuildAllEntities(Guid questionId, CreateSingleQuestionCommand request, Category category, UploadResults uploadResults)
        {
            var now = DateTime.UtcNow;

            //question
            var question = new Question
            {
                Id = questionId,
                CategoryId = request.CategoryId,
                GroupId = null,
                Content = request.Content,
                QuestionType = request.QuestionType,
                DifficultyId = request.DifficultyId,
                ShuffleAnswers = request.ShuffleAnswers,
                DefaultScore = request.DefaultScore,
                Explanation = request.Explanation,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // medias
            var medias = new List<QuestionMedia>();
            int orderIndex = 1;
            if (!string.IsNullOrWhiteSpace(request.AudioUrl))
            {
                medias.Add(new QuestionMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    Url = uploadResults.QuestionAudioUrl,
                    PublicId = uploadResults.QuestionAudioPublicId ?? string.Empty,
                    MediaType = "audio",
                    FileHash = uploadResults.AudioFileHash,
                    OrderIndex = orderIndex++
                });
            }

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                medias.Add(new QuestionMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    Url = uploadResults.QuestionImageUrl,
                    PublicId = uploadResults.QuestionImagePublicId ?? string.Empty,
                    MediaType = "image",
                    FileHash = uploadResults.ImageFileHash,
                    OrderIndex = orderIndex++
                });
            }

            // answer
            var answers = new List<Answer>();
            for (int i = 0; i < request.Answers.Count; i++)
            {
                var answerDto = request.Answers[i];
                var answer = new Answer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    Content = answerDto.Content,
                    IsCorrect = answerDto.IsCorrect,
                    Feedback = answerDto.Feedback,
                    OrderIndex = answerDto.OrderIndex,
                };

                answers.Add(answer);
            }

            // Tags
            var tags = new List<QuestionTag>();
            if (request.Tags != null && request.Tags.Any())
            {
                foreach (var tagName in request.Tags.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(tagName)) continue;

                    tags.Add(new QuestionTag
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        Tag = tagName.Trim(),
                        TagType = "Topic",
                        CreatedAt = now,
                    });
                }
            }

            return (question, medias, answers, tags);
        }
        #endregion


        // ========== Helper: Strip HTML Tags ==========
        //caculate file hash
        private async Task<string> CalculateFileHashAsync(IFormFile file)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = file.OpenReadStream();

            var hashBytes = await sha256.ComputeHashAsync(stream);

            stream.Position = 0;

            return Convert.ToBase64String(hashBytes);
        }

        //Levenshtein with early exit
        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;

            if (s1 == s2) return 1.0;

            var distance = LevenshteinDistance(s1, s2);
            var maxLength = Math.Max(s1.Length, s2.Length);

            return 1.0 - ((double)distance / maxLength);
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1)) return s2?.Length ?? 0;
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j < s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost
                    );
                }
            }

            return matrix[s1.Length, s2.Length];
        }

        private string CreateAnswerSetSignature(List<CreateAnswerWithFileDto> answers)
        {
            var contents = answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => StripHtml(a.Content).Trim().ToLower())
                .ToList();

            return string.Join("|", contents);
        }

        private string CreateAnswerSetSignature(ICollection<Answer> answers)
        {
            var contents = answers
                .OrderBy(a => a.OrderIndex)
                .Select(a => StripHtml(a.Content ?? "").Trim().ToLower())
                .ToList();

            return string.Join("|", contents);
        }
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            return System.Text.RegularExpressions.Regex
                .Replace(html, "<.*?>", string.Empty);
        }
        private class DuplicateInfo
        {
            public Guid QuestionId { get; set; }
            public double Similarity { get; set; }
        }
    }
}