using App.Application.ExamDigitize.Commands;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Questions.Commands
{
    public record SaveDigitizedExamCommand : IRequest<Guid>
    {
        [Required] public Guid CategoryId { get; init; }
        public Guid? DifficultyId { get; init; }

        // ── Option 1: user upload file trực tiếp ──────────────────
        public IFormFile? AudioFile { get; init; }
        public IFormFile? ImageFile { get; init; }

        // ── Option 2: đã có URL (upload trước hoặc từ link ngoài) ─
        public string? AudioUrl { get; init; }
        public string? ImageUrl { get; init; }

        [Required] public ExtractedExamDto ExtractedData { get; init; } = null!;
        public List<string> Tags { get; init; } = [];
    }

    public class SaveDigitizedExamCommandHandler
        : IRequestHandler<SaveDigitizedExamCommand, Guid>
    {
        private readonly IAppDbContext _context;
        private readonly ICloudinaryService _cloudinary; // FIX: inject để upload file

        public SaveDigitizedExamCommandHandler(
            IAppDbContext context,
            ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Guid> Handle(SaveDigitizedExamCommand request, CancellationToken ct)
        {
            // ── Validate ───────────────────────────────────────────
            _ = await _context.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct)
                ?? throw new ValidationException("Danh mục không tồn tại");

            var now = DateTime.UtcNow;
            var data = request.ExtractedData;
            var isPart5 = data.PartNumber == 5;
            var groupId = isPart5 ? Guid.Empty : Guid.NewGuid();

            // ── Upload file lên Cloudinary (nếu có file) ──────────
            // Align với CreateQuestionGroupCommandHandler.UploadAllFilesAsync
            var (finalAudioUrl, finalImageUrl) = await UploadFilesAsync(request, ct);

            // ── QuestionGroup ──────────────────────────────────────
            QuestionGroup? group = isPart5 ? null : new QuestionGroup
            {
                Id = groupId,
                CategoryId = request.CategoryId,
                Content = data.PassageContent,
                Transcript = data.SectionTitle,
                DifficultyId = request.DifficultyId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // ── Group Media ────────────────────────────────────────
            var groupMedias = new List<QuestionGroupMedia>();
            if (!isPart5)
            {
                int mi = 1;
                if (!string.IsNullOrWhiteSpace(finalAudioUrl))
                    groupMedias.Add(BuildGroupMedia(groupId, finalAudioUrl, "audio", mi++, now));
                if (!string.IsNullOrWhiteSpace(finalImageUrl))
                    groupMedias.Add(BuildGroupMedia(groupId, finalImageUrl, "image", mi++, now));
            }

            // ── Questions + Answers + Per-question Media ───────────
            var questions = new List<Question>();
            var answers = new List<Answer>();
            var qMedias = new List<QuestionMedia>();

            foreach (var q in data.Questions.OrderBy(x => x.OrderIndex))
            {
                var qid = Guid.NewGuid();
                var type = (QuestionTypeEnum)q.QuestionType;

                questions.Add(new Question
                {
                    Id = qid,
                    GroupId = isPart5 ? null : groupId,
                    CategoryId = request.CategoryId,
                    Content = q.QuestionText,
                    QuestionType = type,
                    DifficultyId = request.DifficultyId,
                    DefaultScore = 1.0,
                    ShuffleAnswers = false,
                    OrderIndex = q.OrderIndex,
                    IsAiGraded = q.IsAiGraded,
                    SampleAnswer = q.SampleAnswer,
                    MaxWords = q.MaxWords,
                    Explanation = q.Explanation,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                // Per-question media (TOEIC Part 1/2 — audio/image riêng từng câu)
                if (!string.IsNullOrWhiteSpace(q.AudioUrl))
                    qMedias.Add(BuildQMedia(qid, q.AudioUrl, "audio", now));
                if (!string.IsNullOrWhiteSpace(q.ImageUrl))
                    qMedias.Add(BuildQMedia(qid, q.ImageUrl, "image", now));

                // ── Answers ────────────────────────────────────────
                if (IsCompletionType(type))
                {
                    // Fill-in: 1 answer đúng, lọc bỏ rỗng từ Gemini
                    var correct =
                        q.SampleAnswer?.Trim().NullIfEmpty() ??
                        q.Answers.FirstOrDefault(a => a.IsCorrect && !string.IsNullOrWhiteSpace(a.Content))?.Content?.Trim() ??
                        q.Answers.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.Content))?.Content?.Trim();

                    if (correct != null)
                        answers.Add(new Answer
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = qid,
                            Content = correct,
                            IsCorrect = true,
                            OrderIndex = 1,
                            CreatedAt = now,
                            UpdatedAt = now,
                        });
                }
                else
                {
                    // MCQ / TrueFalse / Matching
                    var valid = q.Answers
                        .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                        .OrderBy(a => a.OrderIndex).ToList();

                    for (int i = 0; i < valid.Count; i++)
                        answers.Add(new Answer
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = qid,
                            Content = valid[i].Content.Trim(),
                            IsCorrect = valid[i].IsCorrect,
                            OrderIndex = valid[i].OrderIndex > 0 ? valid[i].OrderIndex : i + 1,
                            CreatedAt = now,
                            UpdatedAt = now,
                        });
                }
            }

            // ── Tags ───────────────────────────────────────────────
            var tags = isPart5 ? [] : request.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t)).Distinct()
                .Select(t => new QuestionTag
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Tag = t.Trim(),
                    TagType = "Topic",
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToList();

            // ── Save ───────────────────────────────────────────────
            using var tx = await _context.BeginTransactionAsync(ct);
            try
            {
                if (group != null) _context.QuestionGroups.Add(group);
                if (groupMedias.Any()) _context.QuestionGroupMedia.AddRange(groupMedias);
                if (tags.Any()) _context.QuestionTags.AddRange(tags);
                if (questions.Any()) _context.Questions.AddRange(questions);
                if (qMedias.Any()) _context.QuestionMedias.AddRange(qMedias);
                if (answers.Any()) _context.Answers.AddRange(answers);

                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return group?.Id ?? Guid.Empty;
            }
            catch { await tx.RollbackAsync(ct); throw; }
        }

        // ── Upload files lên Cloudinary ────────────────────────────
        // Align với CreateQuestionGroupCommandHandler.UploadAllFilesAsync
        private async Task<(string? audioUrl, string? imageUrl)> UploadFilesAsync(
            SaveDigitizedExamCommand request, CancellationToken ct)
        {
            string? audioUrl = request.AudioUrl;
            string? imageUrl = request.ImageUrl;

            var tasks = new List<Task>();

            // Upload audio file nếu có
            if (request.AudioFile != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var result = await _cloudinary.UploadAudioAsync(
                        request.AudioFile, "questions/groups/audio", ct);
                    audioUrl = result.Url;
                }, ct));
            }

            // Upload image file nếu có
            if (request.ImageFile != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var result = await _cloudinary.UploadImageAsync(
                        request.ImageFile, "questions/groups/images", ct);
                    imageUrl = result.Url;
                }, ct));
            }

            await Task.WhenAll(tasks);
            return (audioUrl, imageUrl);
        }

        // ── Type helper ────────────────────────────────────────────
        private static bool IsCompletionType(QuestionTypeEnum t) =>
            t is QuestionTypeEnum.ShortAnswer
              or QuestionTypeEnum.NoteCompletion
              or QuestionTypeEnum.FormCompletion
              or QuestionTypeEnum.TableCompletion
              or QuestionTypeEnum.SummaryCompletion
              or QuestionTypeEnum.SentenceCompletion
              or QuestionTypeEnum.MapLabeling;

        // ── Builder helpers ────────────────────────────────────────
        private static QuestionGroupMedia BuildGroupMedia(
            Guid gid, string url, string type, int order, DateTime now) => new()
            {
                Id = Guid.NewGuid(),
                QuestionGroupId = gid,
                Url = url,
                PublicId = string.Empty,
                MediaType = type,
                OrderIndex = order,
                CreatedAt = now,
                UpdatedAt = now,
            };

        private static QuestionMedia BuildQMedia(
            Guid qid, string url, string type, DateTime now) => new()
            {
                Id = Guid.NewGuid(),
                QuestionId = qid,
                Url = url,
                MediaType = type,
                OrderIndex = 1,
                CreatedAt = now,
                UpdatedAt = now,
            };
    }

    internal static class StringExtensions
    {
        public static string? NullIfEmpty(this string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s;
    }
}