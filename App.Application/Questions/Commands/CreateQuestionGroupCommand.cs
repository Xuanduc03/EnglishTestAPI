using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using App.Domain.Entities;
using CloudinaryDotNet.Actions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Questions.Commands
{
    public record CreateQuestionGroupCommand : IRequest<Guid>
    {
        // nhận tham số từ fe
        public Guid CategoryId { get; set; }
        public string? GroupContent { get; set; } // HTML hoặc Text
        public string? GroupAudioUrl { get; set; } // nhận audio
        public string? GroupImageUrl { get; set; } // nhận group ảnh 
        public Guid? DifficultyId { get; set; }
        public string? Explanation { get; set; }
        public string? Transcript { get; set; }
        public string? MediaJson { get; init; }
        // FILES - Upload trực tiếp
        public IFormFile? GroupAudioFile { get; set; } // Upload audio cho group
        public IFormFile? GroupImageFile { get; set; } // Upload ảnh cho group
        public List<CreateQuestionDto> Questions { get; init; } = [];
        // Tags
        public List<string> Tags { get; init; } = [];
    }

    public class CreateQuestionGroupCommandHandler : IRequestHandler<CreateQuestionGroupCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;

        // constant answer of toeic
        private readonly int TOEIC_ANSWER = 4;
        public CreateQuestionGroupCommandHandler(IAppDbContext conext, ICloudinaryService cloudinary)
        {
            _context = conext;
            _cloudinary = cloudinary;
        }
        public async Task<Guid> Handle(CreateQuestionGroupCommand request, CancellationToken cancellation)
        {
            var category = await ValidateRequestAsync(request, cancellation);
            await CheckDuplicatesAsync(request, category, cancellation);

            var uploadResults = await UploadAllFilesAsync(request, cancellation);

            using var transaction = await _context.BeginTransactionAsync(cancellation);

            try
            {
                var groupId = Guid.NewGuid();

                var entitiesToAdd = BuildAllEntities(
                    groupId,
                    request,
                    category,
                    uploadResults
                );

                // Single batch insert
                _context.QuestionGroups.Add(entitiesToAdd.Group);

                if (entitiesToAdd.GroupMedias.Any())
                    _context.QuestionGroupMedia.AddRange(entitiesToAdd.GroupMedias);

                if (entitiesToAdd.Questions.Any())
                    _context.Questions.AddRange(entitiesToAdd.Questions);

                if (entitiesToAdd.Answers.Any())
                    _context.Answers.AddRange(entitiesToAdd.Answers);


                if (entitiesToAdd.QuestionMedias.Any())
                    _context.QuestionMedias.AddRange(entitiesToAdd.QuestionMedias);

                if (entitiesToAdd.Tags.Any())
                    _context.QuestionTags.AddRange(entitiesToAdd.Tags);

                await _context.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);


                return groupId;

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellation);
                throw;
            }
        }
        #region validate
        // private method 
        private async Task<Category> ValidateRequestAsync(CreateQuestionGroupCommand request, CancellationToken cancellation)
        {
            // check categoryId 
            if (request.CategoryId == Guid.Empty)
                throw new ValidationException("Vui lòng chọn danh mục");

            var categoryIds = new List<Guid> { request.CategoryId };
            if (request.DifficultyId.HasValue && request.DifficultyId.Value != Guid.Empty)
            {
                categoryIds.Add(request.DifficultyId.Value);
            }

            var categories = await _context.Categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c, cancellation);

            if (!categories.TryGetValue(request.CategoryId, out var category))
                throw new ValidationException("Danh mục không tồn tại");

            if (request.DifficultyId.HasValue
              && request.DifficultyId.Value != Guid.Empty
              && !categories.ContainsKey(request.DifficultyId.Value))
            {
                throw new ValidationException("Difficulty không tồn tại");
            }

            // check question 
            if (request.Questions == null || !request.Questions.Any())
            {
                throw new ValidationException("Question group phải có ít nhất 1 câu hỏi");
            }

            // Part 3/4 requires audio
            if (category.Name.Contains("Part 3") || category.Name.Contains("Part 4"))
            {
                if (request.GroupAudioFile == null && string.IsNullOrWhiteSpace(request.GroupAudioUrl))
                    throw new ValidationException($"{category.Name} phải có Audio");
            }

            // Part 6/7 requires content
            if (category.Name.Contains("Part 6") || category.Name.Contains("Part 7"))
            {
                if (string.IsNullOrWhiteSpace(request.GroupContent))
                    throw new ValidationException($"{category.Name} phải có nội dung bài đọc");
            }

            // Validate each question
            for (int i = 0; i < request.Questions.Count; i++)
            {
                ValidateQuestion(request.Questions[i], i + 1);
            }

            return category;
        }


        private void ValidateQuestion(CreateQuestionDto dto, int index)
        {
            // check content
            if (string.IsNullOrEmpty(dto.Content))
            {
                throw new ValidationException("Nội dung câu hỏi không được để trống");
            }

            // check answer count 
            if (dto.Answers.Count != TOEIC_ANSWER)
                throw new ValidationException(
                   $"Câu hỏi {index}: Phải có đúng {TOEIC_ANSWER} đáp án");

            // Check correct answer count
            var correctCount = dto.Answers.Count(a => a.IsCorrect);
            if (correctCount != 1)
                throw new ValidationException(
                    $"Câu hỏi {index}: Phải có đúng 1 đáp án đúng (Tìm thấy {correctCount})");

            // check duplicate orderindex
            var duplicateOrders = dto.Answers
                .GroupBy(a => a.OrderIndex)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicateOrders.Any())
                throw new ValidationException(
                    $"Câu hỏi {index}: OrderIndex bị trùng: {string.Join(", ", duplicateOrders)}");
        }

        #endregion 

        #region Duplicate Check

        private async Task CheckDuplicatesAsync(
            CreateQuestionGroupCommand request,
            Category category,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();

            // ✅ Run all checks in PARALLEL
            var checkTasks = new List<Task<string?>>();

            // 1️⃣ Group content duplicate
            if (!string.IsNullOrWhiteSpace(request.GroupContent))
            {
                var duplicate = await CheckGroupContentDuplicateAsync(
                    request.CategoryId,
                    request.GroupContent,
                    cancellationToken
                );
                if (duplicate != null)
                {
                    errors.Add($"Nội dung nhóm câu hỏi trùng {duplicate.Similarity:P0} với nhóm ID: {duplicate.GroupId}");
                }
            }

            // 2️⃣ Individual question content duplicates
            for (int i = 0; i < request.Questions.Count; i++)
            {
                var question = request.Questions[i];
                var questionIndex = i + 1;

                if (!string.IsNullOrWhiteSpace(question.Content))
                {
                    var duplicate = await CheckQuestionContentDuplicateAsync(
                        request.CategoryId,
                        question.Content,
                        cancellationToken
                    );
                    if (duplicate != null)
                    {
                        errors.Add($"Câu hỏi {questionIndex}: Nội dung trùng {duplicate.Similarity:P0} với câu hỏi ID: {duplicate.QuestionId}");
                    }
                }
            }

            // 3️⃣ Group audio file hash
            if (request.GroupAudioFile != null)
            {
                var audioHash = await CalculateFileHashAsync(request.GroupAudioFile);
                var exists = await _context.QuestionGroupMedia
                    .Where(m => m.FileHash == audioHash && m.MediaType == "audio")
                    .AnyAsync(cancellationToken);

                if (exists)
                {
                    errors.Add("File audio nhóm đã tồn tại trong hệ thống");
                }

            }

            // 4️⃣ Group image file hash
            if (request.GroupImageFile != null)
            {
                var imageHash = await CalculateFileHashAsync(request.GroupImageFile);
                var exists = await _context.QuestionGroupMedia
                    .Where(m => m.FileHash == imageHash && m.MediaType == "image")
                    .AnyAsync(cancellationToken);

                if (exists)
                {
                    errors.Add("File ảnh nhóm đã tồn tại trong hệ thống");
                }
            }

            if (errors.Any())
            {
                throw new ValidationException(
                    $"⚠️ Phát hiện trùng lặp:\n• {string.Join("\n• ", errors)}"
                );
            }
        }

        private async Task<DuplicateInfo?> CheckGroupContentDuplicateAsync(
            Guid categoryId,
            string content,
            CancellationToken cancellationToken)
        {
            var cleanContent = StripHtml(content).Trim().ToLower();

            if (cleanContent.Length < 50) return null; // Group content should be longer

            var existingGroups = await _context.QuestionGroups
                .Where(g => g.CategoryId == categoryId && g.IsActive)
                .OrderByDescending(g => g.CreatedAt)
                .Take(200)
                .Select(g => new { g.Id, g.Content })
                .ToListAsync(cancellationToken);

            foreach (var existing in existingGroups)
            {
                var existingClean = StripHtml(existing.Content ?? "").Trim().ToLower();

                if (Math.Abs(cleanContent.Length - existingClean.Length) > cleanContent.Length * 0.3)
                    continue;

                var similarity = CalculateSimilarity(cleanContent, existingClean);

                if (similarity > 0.85)
                {
                    return new DuplicateInfo
                    {
                        GroupId = existing.Id,
                        Similarity = similarity
                    };
                }
            }

            return null;
        }

        private async Task<DuplicateInfo?> CheckQuestionContentDuplicateAsync(
           Guid categoryId,
           string content,
           CancellationToken cancellationToken)
        {
            var cleanContent = StripHtml(content).Trim().ToLower();

            if (cleanContent.Length < 10) return null;

            var existingQuestions = await _context.Questions
                .Where(q => q.CategoryId == categoryId && q.IsActive)
                .OrderByDescending(q => q.CreatedAt)
                .Take(300)
                .Select(q => new { q.Id, q.Content })
                .ToListAsync(cancellationToken);

            foreach (var existing in existingQuestions)
            {
                var existingClean = StripHtml(existing.Content ?? "").Trim().ToLower();

                if (Math.Abs(cleanContent.Length - existingClean.Length) > cleanContent.Length * 0.2)
                    continue;

                var similarity = CalculateSimilarity(cleanContent, existingClean);

                if (similarity > 0.85)
                {
                    return new DuplicateInfo
                    {
                        QuestionId = existing.Id,
                        Similarity = similarity
                    };
                }
            }

            return null;
        }
        #endregion

        // ========== CLOUDINARY UPLOAD ==========

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
           CancellationToken cancellationToken)
        {
            var results = new UploadResults();
            var uploadTasks = new List<Task>();

            // Group Audio
            if (request.GroupAudioFile != null)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    results.GroupAudioFileHash = await CalculateFileHashAsync(request.GroupAudioFile);
                    var audioResult = await _cloudinary.UploadAudioAsync(
                        request.GroupAudioFile,
                        "toeic/groups/audio",
                        cancellationToken
                    );
                    results.GroupAudioUrl = audioResult.Url;
                    results.GroupAudioPublicId = audioResult.PublicId;
                }, cancellationToken));
            }
            else if (!string.IsNullOrWhiteSpace(request.GroupAudioUrl))
            {
                results.GroupAudioUrl = request.GroupAudioUrl;
            }

            // Group Image
            if (request.GroupImageFile != null)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    results.GroupImageFileHash = await CalculateFileHashAsync(request.GroupImageFile);
                    var imageResult = await _cloudinary.UploadImageAsync(
                        request.GroupImageFile,
                        "toeic/groups/images",
                        cancellationToken
                    );
                    results.GroupImageUrl = imageResult.Url;
                    results.GroupImagePublicId = imageResult.PublicId;
                }, cancellationToken));
            }
            else if (!string.IsNullOrWhiteSpace(request.GroupImageUrl))
            {
                results.GroupImageUrl = request.GroupImageUrl;
            }

            await Task.WhenAll(uploadTasks);

            return results;
        }


        private (QuestionGroup Group,
                  List<QuestionGroupMedia> GroupMedias,
                  List<Question> Questions,
                  List<Answer> Answers,
                  List<QuestionMedia> QuestionMedias,
                  List<QuestionTag> Tags)
             BuildAllEntities(
                 Guid groupId,
                 CreateQuestionGroupCommand request,
                 Category category,
                 UploadResults uploadResults)
        {
            var now = DateTime.UtcNow;
            // Question Group
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
            int mediaOrderIndex = 1;

            if (!string.IsNullOrWhiteSpace(uploadResults.GroupAudioUrl))
            {
                groupMedias.Add(new QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = uploadResults.GroupAudioUrl,
                    PublicId = uploadResults.GroupAudioPublicId ?? string.Empty,
                    MediaType = "audio",
                    OrderIndex = mediaOrderIndex++,
                    FileHash = uploadResults.GroupAudioFileHash
                });
            }

            if (!string.IsNullOrWhiteSpace(uploadResults.GroupImageUrl))
            {
                groupMedias.Add(new QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = uploadResults.GroupImageUrl,
                    PublicId = uploadResults.GroupImagePublicId ?? string.Empty,
                    MediaType = "image",
                    OrderIndex = mediaOrderIndex++,
                    FileHash = uploadResults.GroupImageFileHash
                });
            }


            // Questions, Answers, and Question Medias
            var questions = new List<Question>();
            var allAnswers = new List<Answer>();
            var allQuestionMedias = new List<QuestionMedia>();

            foreach (var questionDto in request.Questions)
            {
                var questionId = Guid.NewGuid();

                var question = new Question
                {
                    Id = questionId,
                    GroupId = groupId,
                    Content = questionDto.Content,
                    QuestionType = questionDto.QuestionType,
                    DifficultyId = request.DifficultyId,
                    Explanation = questionDto.Explanation,
                    DefaultScore = questionDto.DefaultScore,
                    ShuffleAnswers = questionDto.ShuffleAnswers,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                questions.Add(question);


                // Answers
                var answers = questionDto.Answers.Select(a => new Answer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect,
                    Feedback = a.Feedback,
                    OrderIndex = a.OrderIndex,
                }).ToList();
                allAnswers.AddRange(answers);

                // Question Medias (if any)
                if (questionDto.Media?.Any() == true)
                {
                    var medias = questionDto.Media.Select(m => new QuestionMedia
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        Url = m.Url,
                        MediaType = m.MediaType,
                        OrderIndex = m.OrderIndex
                    }).ToList();
                    allQuestionMedias.AddRange(medias);
                }
            }


            // Tags
            var tags = new List<QuestionTag>();
            if (request.Tags?.Any() == true)
            {
                foreach (var tagName in request.Tags.Distinct())
                {
                    if (string.IsNullOrWhiteSpace(tagName)) continue;

                    tags.Add(new QuestionTag
                    {
                        Id = Guid.NewGuid(),
                        QuestionGroupId = groupId,
                        Tag = tagName.Trim(),
                        TagType = "Topic",
                        CreatedAt = now,
                    });
                }
            }
            return (group, groupMedias, questions, allAnswers, allQuestionMedias, tags);
        }

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

            for (int j = 0; j <= s2.Length; j++)
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

        private async Task<string> CalculateFileHashAsync(IFormFile file)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            using var stream = file.OpenReadStream();

            var hashBytes = await sha256.ComputeHashAsync(stream);
            stream.Position = 0;

            return Convert.ToBase64String(hashBytes);
        }
        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            return System.Text.RegularExpressions.Regex
                .Replace(html, "<.*?>", string.Empty);
        }
        private class DuplicateInfo
        {
            public Guid? GroupId { get; set; }
            public Guid? QuestionId { get; set; }
            public double Similarity { get; set; }
        }
    }
}
