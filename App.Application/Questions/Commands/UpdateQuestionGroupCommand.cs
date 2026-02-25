using App.Application.DTOs.Questions;
using App.Application.Interfaces;
using App.Application.Share;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;

namespace App.Application.Questions.Commands
{

    public class UpdateQuestionGroupCommand : IRequest<bool>
    {
        public Guid Id { get; set; } // ID của QuestionGroup cần update
        public Guid CategoryId { get; set; }
        public string? GroupContent { get; set; }
        public string? Explanation { get; set; }
        public Guid? DifficultyId { get; set; }
        public bool IsActive { get; set; } = true;

        // FILES - Upload mới (nếu có)
        public IFormFile? GroupAudioFile { get; set; }
        public IFormFile? GroupImageFile { get; set; }
        // File url đã tồn tại và chỉ cần url
        public string? AudioUrl { get; set; }
        public string? ImaageUrl { get; set; }
        // Flags để xóa file cũ
        public bool DeleteAudio { get; set; } = false;
        public bool DeleteImage { get; set; } = false;

        // Questions (REPLACE toàn bộ)
        public List<UpdateQuestionInGroupDto> Questions { get; set; }
    }


    public class UpdateQuestionInGroupDto
    {
        public Guid? Id { get; set; } // Null = tạo mới, có giá trị = update existing
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "SingleChoice";
        public Guid? DifficultyId { get; set; }
        public double DefaultScore { get; set; } = 1.0;
        public bool ShuffleAnswers { get; set; } = true;
        public string? Explanation { get; set; }
        public List<UpdateAnswerDto>? Answers { get; set; } = [];
        public List<string> Tags { get; set; } = [];
        public List<CreateMediaDto>? Media { get; set; } = [];
    }

    public class UpdateAnswerDto
    {
        public Guid? Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; }
        public int OrderIndex { get; set; }
    }

    public class UpdateQuestionGroupCommandHandler : IRequestHandler<UpdateQuestionGroupCommand, bool>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        private readonly int TOEIC_ANSWER = 4;
        public UpdateQuestionGroupCommandHandler(IAppDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<bool> Handle(
                   UpdateQuestionGroupCommand request,
                   CancellationToken cancellationToken)
        {

            // 1️⃣ VALIDATION & LOAD
            var group = await ValidateAndLoadGroupAsync(request, cancellationToken);

            // 2️⃣ PREPARE MEDIA OPERATIONS (OUTSIDE TRANSACTION)
            var mediaOperations = PrepareGroupMediaOperations(group, request);
            // 3️⃣ UPLOAD NEW FILES (OUTSIDE TRANSACTION)
            var uploadResults = await ExecuteUploadsAsync(mediaOperations, cancellationToken);

            // 4️⃣ DATABASE TRANSACTION (FAST)
            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                bool categoryChanged = group.CategoryId != request.CategoryId;

                UpdateGroupFields(group, request);


                if (categoryChanged)
                {
                    foreach (var q in group.Questions)
                    {
                        q.CategoryId = request.CategoryId;
                    }
                }
             
                // Apply media changes (DB only)
                ApplyMediaChanges(group.Id, uploadResults);

                // Update questions
                await UpdateQuestionsAsync(group, request.Questions, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);


                // 5️⃣ CLEANUP OLD FILES (AFTER successful commit)
                await CleanupOldFilesAsync(mediaOperations);


                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        #region validate
        // ========== VALIDATION ==========
        private async Task<QuestionGroup> ValidateAndLoadGroupAsync(
            UpdateQuestionGroupCommand request,
            CancellationToken cancellationToken)
        {
            // Load group với related questions
            var group = await _context.QuestionGroups
               .Include(g => g.Questions).ThenInclude(q => q.Answers)
                .Include(g => g.Questions).ThenInclude(q => q.Media)
                .Include(g => g.Media)
                .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

            if (group == null)
                throw new Exception("Câu hỏi nhóm không tồn tại");

            // Check CategoryId exists
            if (request.CategoryId != Guid.Empty && request.CategoryId != group.CategoryId)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

                if (!categoryExists)
                    throw new Exception("Danh mục không tồn tại");
            }

            if (request.Questions == null || !request.Questions.Any())
            {
                throw new ValidationException("Nhóm câu hỏi phải có ít nhất 1 câu hỏi");
            }


            // Validate theo Part
            var category = await _context.Categories
             .AsNoTracking()
             .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category?.Name.Contains("Part 3") == true || category?.Name.Contains("Part 4") == true)
            {

                var finalHasAudio = request.GroupAudioFile != null
                                         || (!request.DeleteAudio && group.Media.Any(m => m.MediaType == "audio"))
                                         || !string.IsNullOrEmpty(request.AudioUrl);

                bool isValid = (request.GroupAudioFile != null) || (finalHasAudio && !request.DeleteAudio);

                if (!isValid)
                {
                    throw new ValidationException($"{category.Name} yêu cầu phải có file Audio (Upload mới hoặc giữ file cũ)");
                }
            }


            // Validate each question
            var questionIndex = 0;
            foreach (var q in request.Questions)
            {
                questionIndex++;
                ValidateQuestion(q, questionIndex);
            }

            return group;
        }

        private void ValidateQuestion(UpdateQuestionInGroupDto dto, int index)
        {
            // Check content
            if (string.IsNullOrWhiteSpace(dto.Content))
                throw new ValidationException($"Câu hỏi {index}: Nội dung không được để trống");

            // Check answer count
            if (dto.Answers.Count != TOEIC_ANSWER)
                throw new ValidationException(
                    $"Câu hỏi {index}: Phải có đúng {TOEIC_ANSWER} đáp án (chuẩn TOEIC)");

            // Check correct answer count
            var correctCount = dto.Answers.Count(a => a.IsCorrect);
            if (correctCount != 1)
                throw new ValidationException(
                    $"Câu hỏi {index}: Phải có đúng 1 đáp án đúng (tìm thấy {correctCount})");

            // Check duplicate OrderIndex
            var duplicateOrders = dto.Answers
                .GroupBy(a => a.OrderIndex)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicateOrders.Any())
                throw new ValidationException(
                    $"Câu hỏi {index}: OrderIndex bị trùng: {string.Join(", ", duplicateOrders)}");

            // Check answer content
            foreach (var answer in dto.Answers)
            {
                if (string.IsNullOrWhiteSpace(answer.Content))
                    throw new ValidationException($"Câu hỏi {index}: Nội dung đáp án không được để trống");
            }
        }
        #endregion

        #region Media Operations

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

            // Track old question media for cleanup
            public List<string> OldQuestionMediaPublicIds { get; set; } = new();
        }

        private class UploadResults
        {
            public string? AudioUrl { get; set; }
            public string? AudioPublicId { get; set; }
            public string? ImageUrl { get; set; }
            public string? ImagePublicId { get; set; }
            public List<string> UploadedPublicIds { get; set; } = new();
        }

        // ✅ Step 1: Prepare operations
        private MediaOperations PrepareGroupMediaOperations(
        QuestionGroup group,
        UpdateQuestionGroupCommand request)
        {
            var operations = new MediaOperations();

            // Audio (Group)
            var oldAudioMedia = group.Media?.FirstOrDefault(m => m.MediaType == "audio");
            operations.ShouldDeleteAudio = request .DeleteAudio || request.GroupAudioFile != null;
            operations.ShouldUploadAudio = request.GroupAudioFile != null;
            operations.AudioFileToUpload = request.GroupAudioFile;
            operations.OldAudioPublicId = oldAudioMedia?.PublicId;

            // Image (Group)
            var oldImageMedia = group.Media?.FirstOrDefault(m => m.MediaType == "image");
            operations.ShouldDeleteImage = request.DeleteImage || request.GroupImageFile != null;
            operations.ShouldUploadImage = request.GroupImageFile != null;
            operations.ImageFileToUpload = request.GroupImageFile;
            operations.OldImagePublicId = oldImageMedia?.PublicId;


            return operations;
        }

        // ✅ Step 2: Execute uploads in parallel
        private async Task<UploadResults> ExecuteUploadsAsync(
            MediaOperations operations,
            CancellationToken cancellationToken)
        {
            var results = new UploadResults();
            var uploadTasks = new List<Task>();
            // Upload Audio
            if (operations.ShouldUploadAudio && operations.AudioFileToUpload != null)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    var audioResult = await _cloudinary.UploadAudioAsync(
                        operations.AudioFileToUpload,
                        "toeic/questions/audio",
                        cancellationToken
                    );

                    results.AudioUrl = audioResult.Url;
                    results.AudioPublicId = audioResult.PublicId;
                    results.UploadedPublicIds.Add(audioResult.PublicId);
                }, cancellationToken));
            }

            // Upload Image
            if (operations.ShouldUploadImage && operations.ImageFileToUpload != null)
            {
                uploadTasks.Add(Task.Run(async () =>
                {
                    var imageResult = await _cloudinary.UploadImageAsync(
                        operations.ImageFileToUpload,
                        "toeic/questions/images",
                        cancellationToken
                    );

                    results.ImageUrl = imageResult.Url;
                    results.ImagePublicId = imageResult.PublicId;
                    results.UploadedPublicIds.Add(imageResult.PublicId);
                }, cancellationToken));
            }

            await Task.WhenAll(uploadTasks);

            return results;
        }

        // ✅ Step 3: Apply changes to DB (IN transaction)
        private void UpdateGroupFields(QuestionGroup group, UpdateQuestionGroupCommand request)
        {
            group.CategoryId = request.CategoryId;
            group.Content = request.GroupContent;
            group.Explanation = request.Explanation;
            group.DifficultyId = request.DifficultyId;
            group.IsActive = request.IsActive;
            group.UpdatedAt = DateTime.UtcNow;
        }

        private void ApplyMediaChanges(Guid groupId, UploadResults uploadResults)
        {
            // Remove old media (will be deleted after commit)
            var oldMedia = _context.QuestionGroupMedia
                .Where(m => m.QuestionGroupId == groupId)
                .ToList();

            if (oldMedia.Any())
            {
                _context.QuestionGroupMedia.RemoveRange(oldMedia);
            }

            // Add new audio
            if (!string.IsNullOrWhiteSpace(uploadResults.AudioUrl))
            {
                _context.QuestionGroupMedia.Add(new QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = uploadResults.AudioUrl,
                    PublicId = uploadResults.AudioPublicId ?? string.Empty,
                    MediaType = "audio",
                    OrderIndex = 1
                });
            }

            // Add new image
            if (!string.IsNullOrWhiteSpace(uploadResults.ImageUrl))
            {
                _context.QuestionGroupMedia.Add(new QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = uploadResults.ImageUrl,
                    PublicId = uploadResults.ImagePublicId ?? string.Empty,
                    MediaType = "image",
                    OrderIndex = 2
                });
            }
        }

        // ✅ Step 4: Cleanup old files (AFTER commit)
        private async Task CleanupOldFilesAsync(MediaOperations operations)
        {
            var cleanupTasks = new List<Task>();

            if (operations.ShouldDeleteAudio && !string.IsNullOrWhiteSpace(operations.OldAudioPublicId))
            {
                cleanupTasks.Add(_cloudinary.DeleteAsync(operations.OldAudioPublicId));
            }

            if (operations.ShouldDeleteImage && !string.IsNullOrWhiteSpace(operations.OldImagePublicId))
            {
                cleanupTasks.Add(_cloudinary.DeleteAsync(operations.OldImagePublicId));
            }

            // Cleanup old question media
            foreach (var publicId in operations.OldQuestionMediaPublicIds)
            {
                cleanupTasks.Add(_cloudinary.DeleteAsync(publicId));
            }

            try
            {
                await Task.WhenAll(cleanupTasks);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString(), ex);
            }
        }

        #endregion

        #region Update Questions

        private async Task UpdateQuestionsAsync(
            QuestionGroup group,
            List<UpdateQuestionInGroupDto> questionDtos,
            CancellationToken cancellationToken)
        {

            var existingQuestions = group.Questions.ToList();


            // 1. Identify questions to DELETE (Có trong DB nhưng không có trong JSON gửi lên)
            var inputQuestionIds = questionDtos.Where(q => q.Id.HasValue).Select(q => q.Id.Value).ToList();
            var questionsToDelete = existingQuestions.Where(q => !inputQuestionIds.Contains(q.Id)).ToList();

            if (questionsToDelete.Any())
            {
                // Bạn cần truyền list publicId này ra ngoài hoặc xử lý riêng
                _context.Questions.RemoveRange(questionsToDelete);
            }
            var questionDict = existingQuestions.ToDictionary(q => q.Id);

            // 2. Loop qua JSON để UPDATE hoặc CREATE
            foreach (var dto in questionDtos)
            {
                if (dto.Id.HasValue)
                {
                    // --- UPDATE EXISTING ---
                    if (questionDict.TryGetValue(dto.Id.Value, out var existingQ))
                    {
                        existingQ.Content = dto.Content;
                        existingQ.QuestionType = dto.QuestionType;
                        existingQ.DifficultyId = dto.DifficultyId;
                        existingQ.Explanation = dto.Explanation;
                        existingQ.DefaultScore = dto.DefaultScore;
                        existingQ.ShuffleAnswers = dto.ShuffleAnswers;
                        existingQ.UpdatedAt = DateTime.UtcNow;

                        if (dto.Answers != null)
                        {
                            var existingAnswers = existingQ.Answers.ToList();
                            var inputAnswers = dto.Answers.Where(a => a.Id.HasValue).Select(a => a.Id.Value).ToList();

                            var toDelete = existingAnswers.Where(a => !inputAnswers.Contains(a.Id)).ToList();
                            _context.Answers.RemoveRange(toDelete);

                            // Bước 3: Duyệt qua list FE để UPDATE hoặc ADD
                            foreach (var aDto in dto.Answers)
                            {
                                var dbAnswer = existingAnswers.FirstOrDefault(a => a.Id == aDto.Id);

                                if (dbAnswer != null)
                                {
                                    // ĐÂY MỚI LÀ UPDATE: Giữ nguyên ID, chỉ đổi giá trị
                                    dbAnswer.Content = aDto.Content;
                                    dbAnswer.IsCorrect = aDto.IsCorrect;
                                    dbAnswer.OrderIndex = aDto.OrderIndex;
                                    dbAnswer.UpdatedAt = DateTime.UtcNow;
                                }
                                else
                                {
                                    // ĐÂY LÀ ADD NEW: Dành cho câu trả lời mới thêm trên giao diện
                                    existingQ.Answers.Add(new Answer
                                    {
                                        Id = Guid.NewGuid(),
                                        Content = aDto.Content,
                                        IsCorrect = aDto.IsCorrect,
                                        QuestionId = existingQ.Id,
                                        CreatedAt = DateTime.UtcNow
                                    });
                                }
                            }
                        }

                        if (dto.Media != null)
                        {
                            var existingMedia = existingQ.Media.ToList();
                            var inputMediaIds = dto.Media.Where(m => m.Id.HasValue).Select(m => m.Id.Value).ToList();

                            // 1. XÓA: Những thằng có trong DB nhưng không có trong danh sách gửi lên
                            var mediaToDelete = existingMedia.Where(m => !inputMediaIds.Contains(m.Id)).ToList();
                            _context.QuestionMedias.RemoveRange(mediaToDelete);

                            // 2. DUYỆT ĐỂ UPDATE HOẶC ADD
                            foreach (var mDto in dto.Media)
                            {
                                var dbMedia = existingMedia.FirstOrDefault(m => m.Id == mDto.Id);

                                if (dbMedia != null)
                                {
                                    // UPDATE: Giữ nguyên ID, chỉ cập nhật thông tin thay đổi
                                    dbMedia.Url = mDto.Url;
                                    dbMedia.PublicId = mDto.PublicId ?? string.Empty;
                                    dbMedia.MediaType = mDto.MediaType;
                                    dbMedia.OrderIndex = mDto.OrderIndex;
                                    dbMedia.UpdatedAt = DateTime.UtcNow;
                                }
                                else
                                {
                                    // ADD NEW: Thêm mới hoàn toàn
                                    existingQ.Media.Add(new QuestionMedia
                                    {
                                        Id = Guid.NewGuid(),
                                        QuestionId = existingQ.Id,
                                        Url = mDto.Url,
                                        PublicId = mDto.PublicId ?? string.Empty,
                                        MediaType = mDto.MediaType,
                                        OrderIndex = mDto.OrderIndex,
                                        CreatedAt = DateTime.UtcNow
                                    });
                                }
                            }
                        }
                    }
                }
                else
                {
                    // --- CREATE NEW ---
                    var newQ = CreateQuestion(dto, group.Id);
                    _context.Questions.Add(newQ);
                }
            }
        }

        private Question CreateQuestion(
            UpdateQuestionInGroupDto dto,
            Guid groupId)
        {
            var now = DateTime.UtcNow;

            var question = new Question
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Content = dto.Content,
                QuestionType = dto.QuestionType,
                DifficultyId = dto.DifficultyId,
                Explanation = dto.Explanation,
                DefaultScore = dto.DefaultScore,
                ShuffleAnswers = dto.ShuffleAnswers,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // Add Answers
            question.Answers = dto.Answers.Select(a => new Answer
            {
                Id = Guid.NewGuid(),
                QuestionId = question.Id,
                Content = a.Content,
                IsCorrect = a.IsCorrect,
                Feedback = a.Feedback,
                OrderIndex = a.OrderIndex,
            }).ToList();

            // Add Media
            if (dto.Media?.Any() == true)
            {
                question.Media = dto.Media.Select(m => new QuestionMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Url = m.Url,
                    PublicId = m.PublicId ?? string.Empty,
                    MediaType = m.MediaType,
                    OrderIndex = m.OrderIndex
                }).ToList();
            }

            return question;
        }

        #endregion

    }

}
 