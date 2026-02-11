using App.Application.Interfaces;
using App.Application.Questions.Commands;
using App.Application.Questions.Dtos;
using App.Application.Questions.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Services
{
    public class ExcelZipImportService : IExcelZipImportService
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        private readonly ILogger<ExcelZipImportService> _logger;

        private readonly string[] AllowedAudioExt = [".mp3", ".wav", ".m4a"];
        private readonly string[] AllowedImageExt = [".jpg", ".jpeg", ".png", ".webp"];

        public ExcelZipImportService(
            IAppDbContext context,
            ICloudinaryService cloudinary,
            ILogger<ExcelZipImportService> logger)
        {
            _context = context;
            _cloudinary = cloudinary;
            _logger = logger;
        }


        public async Task<ImportQuestionExcelResult> ImportAsync(
            ExcelZipParseResult parseResult,
            ImportOptions options,
            CancellationToken ct)
        {
            var result = new ImportQuestionExcelResult
            {
                Success = false,
                Message = "Đang import dữ liệu..."
            };

            if (parseResult.HasError)
                throw new InvalidOperationException(
                    "ParseResult còn lỗi – không thể import");

            //   1️⃣ Upload media TRƯỚC, BÊN NGOÀI transaction
            IDictionary<string, string> uploadedMediaUrls;
            try
            {
                uploadedMediaUrls = await UploadAllMediaAsync(
                    parseResult.MediaIndex,
                    ct);

                //   Validate upload thành công
                if (uploadedMediaUrls.Count != parseResult.MediaIndex.Count)
                {
                    var missing = parseResult.MediaIndex.Keys
                        .Except(uploadedMediaUrls.Keys, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    throw new InvalidOperationException(
                        $"Upload thiếu {missing.Count} media: {string.Join(", ", missing)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Media upload failed - aborting import");

                result.Success = false;
                result.Message = $"Upload media thất bại: {ex.Message}";
                return result;
            }

            // 2️⃣ Build entities
            var questionsToAdd = new List<Domain.Entities.Question>();
            var answersToAdd = new List<Domain.Entities.Answer>();
            var groupsToAdd = new List<Domain.Entities.QuestionGroup>();
            var groupQuestionsToAdd = new List<Domain.Entities.Question>();
            var groupAnswersToAdd = new List<Domain.Entities.Answer>();
            var questionMediasToAdd = new List<Domain.Entities.QuestionMedia>();
            var groupMediasToAdd = new List<Domain.Entities.QuestionGroupMedia>();

            int importedCount = 0;
            int skippedCount = 0;

            foreach (var sheet in parseResult.Sheets)
            {
                foreach (var rawItem in sheet.Items)
                {
                    // ===== SINGLE QUESTION =====
                    if (rawItem is QuestionPreviewDto q)
                    {
                        if (q.HasError)
                        {
                            skippedCount++;
                            result.TotalFailed++;
                            result.FailedItems.Add(new ImportFailedItemDto
                            {
                                SheetName = sheet.SheetName,
                                Reason = "Câu hỏi có lỗi từ preview",
                                Details = q.Errors.Select(e => e.Message).ToList()
                            });
                            continue;
                        }

                        BuildSingleQuestion(
                            q,
                            uploadedMediaUrls,
                            questionsToAdd,
                            answersToAdd,
                            questionMediasToAdd);

                        importedCount++;
                    }

                    // ===== QUESTION GROUP =====
                    else if (rawItem is QuestionGroupPreviewDto g)
                    {
                        if (g.HasError)
                        {
                            skippedCount++;
                            result.FailedItems.Add(new ImportFailedItemDto
                            {
                                SheetName = sheet.SheetName,
                                Reason = "Nhóm câu hỏi có lỗi từ preview",
                                Details = g.Errors.Select(e => e.Message).ToList()
                            });
                            continue;
                        }

                        BuildQuestionGroup(
                            g,
                            uploadedMediaUrls,
                            groupsToAdd,
                            groupQuestionsToAdd,
                            groupAnswersToAdd,
                            groupMediasToAdd);

                        importedCount++; // 1 group = 1 item
                    }
                }
            }


            // 3️⃣ Save DB (transaction)
            using var tx = await _context.BeginTransactionAsync(ct);
            try
            {
                if (groupsToAdd.Any()) _context.QuestionGroups.AddRange(groupsToAdd);
                if (questionsToAdd.Any()) _context.Questions.AddRange(questionsToAdd);
                if (groupQuestionsToAdd.Any()) _context.Questions.AddRange(groupQuestionsToAdd);
                if (answersToAdd.Any()) _context.Answers.AddRange(answersToAdd);
                if (groupAnswersToAdd.Any()) _context.Answers.AddRange(groupAnswersToAdd);
                if (questionMediasToAdd.Any()) _context.QuestionMedias.AddRange(questionMediasToAdd);
                if (groupMediasToAdd.Any()) _context.QuestionGroupMedia.AddRange(groupMediasToAdd);

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                result.Success = true;
                result.TotalImported =
                    questionsToAdd.Count + groupsToAdd.Count;
                result.TotalSkipped = skippedCount;
                result.Message =
                      $"Import xong: {result.TotalImported} thành công, {result.TotalSkipped} bỏ qua, {result.TotalFailed} lỗi";

            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "Import failed");

                result.Success = false;
                result.Message = $"Lỗi import: {ex.Message}";
            }

            return result;
        }

        // ================= MEDIA =================

        private async Task<IDictionary<string, string>> UploadAllMediaAsync(
      Dictionary<string, string> mediaIndex,
      CancellationToken ct)
        {
            var uploaded = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var errors = new ConcurrentBag<string>();

            var tasks = mediaIndex.Select(async kv =>
            {
                var (fileName, filePath) = kv;
                var normalizedKey = NormalizeFileName(fileName);

                try
                {
                    var ext = Path.GetExtension(filePath).ToLowerInvariant();

                    if (AllowedAudioExt.Contains(ext))
                    {
                        var res = await _cloudinary.UploadAudioAsync(filePath, "toeic/single/audio", ct);
                        uploaded[normalizedKey] = res.Url;
                        _logger.LogInformation($"  Uploaded audio: {fileName}");
                    }
                    else if (AllowedImageExt.Contains(ext))
                    {
                        var res = await _cloudinary.UploadImageAsync(filePath, "toeic/single/image", ct);
                        uploaded[normalizedKey] = res.Url;
                        _logger.LogInformation($"  Uploaded image: {fileName}");
                    }
                    else
                    {
                        errors.Add($"{fileName}: unsupported extension {ext}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Upload failed: {fileName}");
                    errors.Add($"{fileName}: {ex.Message}");
                }
            }).ToList();

            await Task.WhenAll(tasks);

            //   Check errors
            if (errors.Any())
            {
                throw new InvalidOperationException(
                    $"Upload failed for {errors.Count} files:\n{string.Join("\n", errors)}");
            }

            _logger.LogInformation($"  Total media uploaded: {uploaded.Count}/{mediaIndex.Count}");
            return uploaded;
        }

        // ================= BUILD =================

        private void BuildSingleQuestion(
            QuestionPreviewDto dto,
            IDictionary<string, string> mediaUrls,
            List<Domain.Entities.Question> questions,
            List<Domain.Entities.Answer> answers,
            List<Domain.Entities.QuestionMedia> medias)
        {
            var qId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            questions.Add(new Domain.Entities.Question
            {
                Id = qId,
                CategoryId = dto.CategoryId,
                Content = dto.Content,
                QuestionType = "SingleChoice",
                DifficultyId = dto.DifficultyId,
                DefaultScore = 1,
                ShuffleAnswers = true,
                Explanation = dto.Explanation,
                IsActive = true,
                CreatedAt = now
            });

            foreach (var a in dto.Answers)
            {
                answers.Add(new Domain.Entities.Answer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = qId,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect,
                    OrderIndex = a.OrderIndex
                });
            }

            TryAddMedia(dto.AudioFileName, "audio", qId, mediaUrls, medias);
            TryAddMedia(dto.ImageFileName, "image", qId, mediaUrls, medias);
        }

        private void BuildQuestionGroup(
            QuestionGroupPreviewDto dto,
            IDictionary<string, string> mediaUrls,
            List<Domain.Entities.QuestionGroup> groups,
            List<Domain.Entities.Question> questions,
            List<Domain.Entities.Answer> answers,
            List<Domain.Entities.QuestionGroupMedia> medias)
        {
            var groupId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            groups.Add(new Domain.Entities.QuestionGroup
            {
                Id = groupId,
                CategoryId = dto.CategoryId,
                Content = dto.GroupContent,
                Explanation = dto.Explanation,
                IsActive = true,
                CreatedAt = now
            });

            if (!string.IsNullOrWhiteSpace(dto.AudioFileName)
                && mediaUrls.TryGetValue(dto.AudioFileName, out var audio))
            {
                medias.Add(new Domain.Entities.QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = audio,
                    MediaType = "audio",
                    OrderIndex = 1
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.ImageFileName)
             && mediaUrls.TryGetValue(dto.ImageFileName, out var image))
            {
                medias.Add(new Domain.Entities.QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = image,
                    MediaType = "image",
                    OrderIndex = 2
                });
            }


            foreach (var q in dto.Questions)
            {
                var qId = Guid.NewGuid();
                questions.Add(new Domain.Entities.Question
                {
                    Id = qId,
                    GroupId = groupId,
                    CategoryId = dto.CategoryId,
                    Content = q.Content,
                    Explanation = q.Explanation,
                    DifficultyId = q.DifficultyId,
                    QuestionType = "SingleChoice",
                    DefaultScore = 1,
                    ShuffleAnswers = true,
                    IsActive = true,
                    CreatedAt = now
                });

                foreach (var a in q.Answers)
                {
                    answers.Add(new Domain.Entities.Answer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = qId,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect,
                        OrderIndex = a.OrderIndex
                    });
                }
            }
        }

        private string NormalizeFileName(string name) =>
    (name ?? "").Trim().ToLowerInvariant();

        // ===== ExcelZipImportService.cs =====
        private void TryAddMedia(
            string fileName,
            string type,
            Guid questionId,
            IDictionary<string, string> mediaUrls,
            List<Domain.Entities.QuestionMedia> medias)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var normalizedKey = NormalizeFileName(fileName);

            //   Try lookup: exact match, with extension, without extension
            if (!mediaUrls.TryGetValue(normalizedKey, out var url))
            {
                //   Thử thêm extension nếu chưa có
                var ext = type == "audio"
                    ? new[] { ".mp3", ".wav", ".m4a" }
                    : new[] { ".jpg", ".jpeg", ".png", ".webp" };

                foreach (var e in ext)
                {
                    if (mediaUrls.TryGetValue(normalizedKey + e, out url))
                    {
                        _logger.LogDebug($"  Found media with extension: {fileName} -> {normalizedKey + e}");
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(url))
            {
                medias.Add(new Domain.Entities.QuestionMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    Url = url,
                    MediaType = type,
                    OrderIndex = type == "audio" ? 1 : 2
                });
                _logger.LogInformation($"  Added media: {fileName} -> {url}");
            }
            else
            {
                var availableKeys = string.Join(", ", mediaUrls.Keys.Take(10));
                _logger.LogWarning($"⚠️ Media not found: '{fileName}' (normalized: '{normalizedKey}'). Available keys: {availableKeys}");
            }
        }
    }
}

