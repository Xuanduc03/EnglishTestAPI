using App.Application.ExamDigitize.Commands;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Questions.Commands
{
    public record SaveDigitizedExamCommand : IRequest<Guid>
    {
        // Category: Part/Section/Passage ID
        [Required]
        public Guid CategoryId { get; init; }

        public Guid? DifficultyId { get; init; }

        // Audio URL cho Listening (đã upload trước)
        public string? AudioUrl { get; init; }

        // Dữ liệu đã extract và confirm từ FE
        [Required]
        public ExtractedExamDto ExtractedData { get; init; } = null!;

        public List<string> Tags { get; init; } = [];
    }

    public class SaveDigitizedExamCommandHandler
        : IRequestHandler<SaveDigitizedExamCommand, Guid>
    {
        private readonly IAppDbContext _context;

        public SaveDigitizedExamCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(
            SaveDigitizedExamCommand request,
            CancellationToken cancellationToken)
        {
            // Validate category tồn tại
            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken)
                ?? throw new ValidationException("Danh mục không tồn tại");

            var now = DateTime.UtcNow;
            var groupId = Guid.NewGuid();
            var data = request.ExtractedData;

            // ── Tạo QuestionGroup ─────────────────────────────────
            var group = new QuestionGroup
            {
                Id = groupId,
                CategoryId = request.CategoryId,
                Content = data.PassageContent,   // Reading passage
                Transcript = data.SectionTitle,     // Listening section title
                DifficultyId = request.DifficultyId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // ── Group Media (audio cho Listening) ─────────────────
            var groupMedias = new List<QuestionGroupMedia>();
            if (!string.IsNullOrWhiteSpace(request.AudioUrl))
            {
                groupMedias.Add(new QuestionGroupMedia
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Url = request.AudioUrl,
                    PublicId = string.Empty,
                    MediaType = "audio",
                    OrderIndex = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            // ── Questions ─────────────────────────────────────────
            var questions = new List<Question>();
            var answers = new List<Answer>();

            foreach (var q in data.Questions.OrderBy(x => x.OrderIndex))
            {
                var questionId = Guid.NewGuid();

                questions.Add(new Question
                {
                    Id = questionId,
                    GroupId = groupId,
                    CategoryId = request.CategoryId,
                    Content = q.QuestionText,
                    QuestionType = (QuestionTypeEnum)q.QuestionType,
                    DifficultyId = request.DifficultyId,
                    DefaultScore = 1.0,
                    ShuffleAnswers = false,
                    OrderIndex = q.OrderIndex,
                    IsAiGraded = q.IsAiGraded,
                    SampleAnswer = q.SampleAnswer,
                    MaxWords = q.MaxWords,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });

                // Answers
                foreach (var a in q.Answers.OrderBy(x => x.OrderIndex))
                {
                    answers.Add(new Answer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect,
                        OrderIndex = a.OrderIndex,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                // Fill-in: lưu sampleAnswer như 1 answer IsCorrect=true
                if (q.IsAiGraded && !string.IsNullOrWhiteSpace(q.SampleAnswer))
                {
                    answers.Add(new Answer
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = questionId,
                        Content = q.SampleAnswer,
                        IsCorrect = true,
                        OrderIndex = 1,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
            }

            // ── Tags 
            var tags = request.Tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .Select(t => new QuestionTag
                {
                    Id = Guid.NewGuid(),
                    QuestionGroupId = groupId,
                    Tag = t.Trim(),
                    TagType = "Topic",
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToList();

            // ── Save 
            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.QuestionGroups.Add(group);
                if (groupMedias.Any()) _context.QuestionGroupMedia.AddRange(groupMedias);
                if (questions.Any()) _context.Questions.AddRange(questions);
                if (answers.Any()) _context.Answers.AddRange(answers);
                if (tags.Any()) _context.QuestionTags.AddRange(tags);

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return groupId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
