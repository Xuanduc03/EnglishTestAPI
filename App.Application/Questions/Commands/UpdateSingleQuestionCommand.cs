using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Questions.Commands
{
    public class UpdateSingleQuestionCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string? Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "SingleChoice";
        public Guid? DifficultyId { get; set; }
        public double DefaultScore { get; set; } = 1.0;
        public bool ShuffleAnswers { get; set; } = true;
        public string? Explanation { get; set; }
        public bool IsActive { get; set; } = true;
        public List<string>? Tags { get; set; }

        // Files (chỉ dùng khi upload file)
        public IFormFile? AudioFile { get; set; }
        public IFormFile? ImageFile { get; set; }
        public bool DeleteAudio { get; set; } = false;
        public bool DeleteImage { get; set; } = false;

        // Answers
        public List<UpdateAnswerQuesion> Answers { get; set; } = [];
    }

    public class UpdateAnswerQuesion {
        public Guid? Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; }
        public int OrderIndex { get; set; }
    }


    // ============================================
    // HANDLER
    // ============================================
    public class UpdateSingleQuestionCommandHandler
        : IRequestHandler<UpdateSingleQuestionCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;

        public UpdateSingleQuestionCommandHandler(IAppDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Guid> Handle(
            UpdateSingleQuestionCommand request,
            CancellationToken cancellationToken)
        {
            // 1️⃣ VALIDATE INPUT
            ValidateInput(request);

            // 2️⃣ LOAD & VALIDATE ENTITY
            var question = await ValidateAndLoadQuestionAsync(request, cancellationToken);

            // 3️⃣ PREPARE + UPLOAD MEDIA (nếu có file)
            var mediaOps = PrepareMediaOperations(question, request);
            var uploadResults = await ExecuteUploadsAsync(mediaOps, cancellationToken);

            // 4️⃣ TRANSACTION
            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                UpdateQuestionFields(question, request);
                ApplyMediaChanges(question, mediaOps, uploadResults);
                await UpdateAnswersAsync(question, request.Answers, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // 5️⃣ CLEANUP old files sau commit
                await CleanupOldFilesAsync(mediaOps);

                return question.Id;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                await CleanupUploadedFilesAsync(uploadResults);
                throw;
            }
        }

        #region Validate

        private void ValidateInput(UpdateSingleQuestionCommand request)
        {
            if (request.Id == Guid.Empty)
                throw new Exception("ID câu hỏi không hợp lệ");

            if (request.CategoryId == Guid.Empty)
                throw new Exception("CategoryId không hợp lệ");

            if (request.Answers == null || !request.Answers.Any())
                throw new Exception("Phải có ít nhất 1 đáp án");

            var correctCount = request.Answers.Count(a => a.IsCorrect);
            if (correctCount != 1)
                throw new Exception($"Phải có đúng 1 đáp án đúng (tìm thấy {correctCount})");

            var duplicateOrders = request.Answers
                .GroupBy(a => a.OrderIndex)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateOrders.Any())
                throw new Exception($"OrderIndex bị trùng: {string.Join(", ", duplicateOrders)}");
        }

        private async Task<Question> ValidateAndLoadQuestionAsync(
            UpdateSingleQuestionCommand request,
            CancellationToken cancellationToken)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .Include(q => q.Media)
                .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

            if (question == null)
                throw new Exception($"Câu hỏi không tồn tại (ID: {request.Id})");

            if (question.GroupId != null)
                throw new Exception("Không thể update câu hỏi thuộc nhóm bằng endpoint này");

            // Validate Category
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
                throw new Exception($"Danh mục không tồn tại (ID: {request.CategoryId})");

            // Validate Difficulty (nếu có)
            if (request.DifficultyId.HasValue && request.DifficultyId.Value != Guid.Empty)
            {
                var diffExists = await _context.Categories
                    .AsNoTracking()
                    .AnyAsync(d => d.Id == request.DifficultyId.Value, cancellationToken);

                if (!diffExists)
                    throw new Exception($"Độ khó không tồn tại (ID: {request.DifficultyId})");
            }

            // Validate theo Part
            ValidateByPart(question, request, category);

            return question;
        }

        private void ValidateByPart(
            Question question,
            UpdateSingleQuestionCommand request,
            Category category)
        {
            bool isPart1 = category.Name.Contains("Part 1");
            bool isPart2 = category.Name.Contains("Part 2");

            if (!isPart1 && string.IsNullOrWhiteSpace(request.Content))
                throw new Exception("Nội dung câu hỏi không được để trống");

            if (isPart1)
            {
                bool hasAudio = request.AudioFile != null
                    || question.Media.Any(m => m.MediaType == "Audio" && !request.DeleteAudio);
                bool hasImage = request.ImageFile != null
                    || question.Media.Any(m => m.MediaType == "Image" && !request.DeleteImage);

                if (!hasAudio) throw new Exception("Part 1 bắt buộc có Audio");
                if (!hasImage) throw new Exception("Part 1 bắt buộc có Hình ảnh");
            }

            if (isPart2)
            {
                bool hasAudio = request.AudioFile != null
                    || question.Media.Any(m => m.MediaType == "Audio" && !request.DeleteAudio);

                if (!hasAudio) throw new Exception("Part 2 bắt buộc có Audio");
            }

            int expectedCount = isPart2 ? 3 : 4;
            if (request.Answers.Count != expectedCount)
                throw new Exception($"{category.Name} phải có đúng {expectedCount} đáp án");
        }

        #endregion

        #region Media

        private class MediaOperations
        {
            public bool ShouldDeleteAudio { get; set; }
            public bool ShouldUploadAudio { get; set; }
            public IFormFile? AudioFileToUpload { get; set; }
            public string? OldAudioPublicId { get; set; }

            public bool ShouldDeleteImage { get; set; }
            public bool ShouldUploadImage { get; set; }
            public IFormFile? ImageFileToUpload { get; set; }
            public string? OldImagePublicId { get; set; }

            public List<string> OldAnswerAudioPublicIds { get; set; } = new();
        }

        private class UploadResults
        {
            public string? AudioUrl { get; set; }
            public string? AudioPublicId { get; set; }
            public string? ImageUrl { get; set; }
            public string? ImagePublicId { get; set; }
            public List<string> UploadedPublicIds { get; set; } = new();
        }

        private MediaOperations PrepareMediaOperations(
            Question question,
            UpdateSingleQuestionCommand request)
        {
            var ops = new MediaOperations();

            var oldAudio = question.Media?.FirstOrDefault(m => m.MediaType.ToLower() == "audio");
            ops.OldAudioPublicId = oldAudio?.PublicId;
            ops.ShouldUploadAudio = request.AudioFile != null;
            ops.AudioFileToUpload = request.AudioFile;
            ops.ShouldDeleteAudio = request.DeleteAudio || request.AudioFile != null;

            var oldImage = question.Media?.FirstOrDefault(m => m.MediaType.ToLower() == "image");
            ops.OldImagePublicId = oldImage?.PublicId;
            ops.ShouldUploadImage = request.ImageFile != null;
            ops.ImageFileToUpload = request.ImageFile;
            ops.ShouldDeleteImage = request.DeleteImage || request.ImageFile != null;

            return ops;
        }

        private async Task<UploadResults> ExecuteUploadsAsync(
            MediaOperations ops,
            CancellationToken cancellationToken)
        {
            var results = new UploadResults();
            var tasks = new List<Task>();

            if (ops.ShouldUploadAudio && ops.AudioFileToUpload != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var res = await _cloudinary.UploadAudioAsync(
                        ops.AudioFileToUpload, "toeic/single/audio", cancellationToken);
                    results.AudioUrl = res.Url;
                    results.AudioPublicId = res.PublicId;
                    results.UploadedPublicIds.Add(res.PublicId);
                }, cancellationToken));
            }

            if (ops.ShouldUploadImage && ops.ImageFileToUpload != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var res = await _cloudinary.UploadImageAsync(
                        ops.ImageFileToUpload, "toeic/single/image", cancellationToken);
                    results.ImageUrl = res.Url;
                    results.ImagePublicId = res.PublicId;
                    results.UploadedPublicIds.Add(res.PublicId);
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);
            return results;
        }

        private void ApplyMediaChanges(
            Question question,
            MediaOperations ops,
            UploadResults results)
        {
            var oldAudio = question.Media?.FirstOrDefault(m => m.MediaType == "Audio");
            if (oldAudio != null && (ops.ShouldDeleteAudio || results.AudioUrl != null))
                _context.QuestionMedias.Remove(oldAudio);

            if (!string.IsNullOrWhiteSpace(results.AudioUrl))
            {
                _context.QuestionMedias.Add(new QuestionMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Url = results.AudioUrl,
                    PublicId = results.AudioPublicId ?? string.Empty,
                    MediaType = "Audio",
                    OrderIndex = 1
                });
            }

            var oldImage = question.Media?.FirstOrDefault(m => m.MediaType == "Image");
            if (oldImage != null && (ops.ShouldDeleteImage || results.ImageUrl != null))
                _context.QuestionMedias.Remove(oldImage);

            if (!string.IsNullOrWhiteSpace(results.ImageUrl))
            {
                _context.QuestionMedias.Add(new QuestionMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Url = results.ImageUrl,
                    PublicId = results.ImagePublicId ?? string.Empty,
                    MediaType = "Image",
                    OrderIndex = 2
                });
            }
        }

        private async Task CleanupOldFilesAsync(MediaOperations ops)
        {
            var tasks = new List<Task>();

            if (ops.ShouldDeleteAudio && !string.IsNullOrWhiteSpace(ops.OldAudioPublicId))
                tasks.Add(_cloudinary.DeleteAsync(ops.OldAudioPublicId));

            if (ops.ShouldDeleteImage && !string.IsNullOrWhiteSpace(ops.OldImagePublicId))
                tasks.Add(_cloudinary.DeleteAsync(ops.OldImagePublicId));

            foreach (var id in ops.OldAnswerAudioPublicIds)
                tasks.Add(_cloudinary.DeleteAsync(id));

            if (tasks.Any())
                await Task.WhenAll(tasks);
        }

        private async Task CleanupUploadedFilesAsync(UploadResults results)
        {
            if (results.UploadedPublicIds.Any())
                await Task.WhenAll(results.UploadedPublicIds.Select(id => _cloudinary.DeleteAsync(id)));
        }

        #endregion

        #region Update

        private void UpdateQuestionFields(Question question, UpdateSingleQuestionCommand request)
        {
            question.CategoryId = request.CategoryId;
            question.Content = request.Content;
            question.QuestionType = request.QuestionType;
            question.DifficultyId = request.DifficultyId;
            question.DefaultScore = request.DefaultScore;
            question.ShuffleAnswers = request.ShuffleAnswers;
            question.Explanation = request.Explanation;
            question.IsActive = request.IsActive;
            question.UpdatedAt = DateTime.UtcNow;
        }

        private async Task UpdateAnswersAsync(
            Question question,
            List<UpdateAnswerQuesion> answerDtos,
            CancellationToken cancellationToken)
        {
            var existingMap = question.Answers
                ?.ToDictionary(a => a.Id) ?? new Dictionary<Guid, Answer>();

            var toRemove = new List<Answer>(question.Answers ?? new List<Answer>());

            foreach (var dto in answerDtos)
            {
                if (dto.Id.HasValue
                    && dto.Id.Value != Guid.Empty
                    && existingMap.TryGetValue(dto.Id.Value, out var existing))
                {
                    existing.Content = dto.Content;
                    existing.IsCorrect = dto.IsCorrect;
                    existing.Feedback = dto.Feedback;
                    existing.OrderIndex = dto.OrderIndex;
                    toRemove.Remove(existing);
                }
                else
                {
                    _context.Answers.Add(new Answer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = question.Id,
                        Content = dto.Content,
                        IsCorrect = dto.IsCorrect,
                        Feedback = dto.Feedback,
                        OrderIndex = dto.OrderIndex
                    });
                }
            }

            if (toRemove.Any())
                _context.Answers.RemoveRange(toRemove);
        }

        #endregion
    }
}