using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Exams.Queries
{
    public class GetExamPreviewQuery : IRequest<ExamPreviewDto>
    {
        public Guid ExamId { get; set; }
        public bool ShowCorrectAnswers { get; set; } = true;
    }

    public class GetExamPreviewQueryHandler : IRequestHandler<GetExamPreviewQuery, ExamPreviewDto>
    {
        private readonly IAppDbContext _context;

        public GetExamPreviewQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<ExamPreviewDto> Handle(
            GetExamPreviewQuery request,
            CancellationToken cancellationToken)
        {
            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Id == request.ExamId && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException($"Exam {request.ExamId} not found");

            var sections = await _context.ExamSections
                    .AsNoTracking()
                    .Where(s => s.ExamId == request.ExamId && !s.IsDeleted)
                    .Include(s => s.Category)
                    .Include(s => s.ExamQuestions.Where(eq => !eq.IsDeleted))
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Answers.OrderBy(a => a.OrderIndex))
                    .Include(s => s.ExamQuestions.Where(eq => !eq.IsDeleted))
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Media) 
                    .Include(s => s.ExamQuestions.Where(eq => !eq.IsDeleted))
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Group)
                                .ThenInclude(g => g.Media)
                    .OrderBy(s => s.OrderIndex)
                    .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var timeLimitSeconds = exam.Duration * 60;

            return new ExamPreviewDto
            {
                AttemptId = Guid.Empty,
                IsPreview = true,
                ShowCorrectAnswers = request.ShowCorrectAnswers,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                ExamCode = exam.Code,
                Status = exam.Status.ToString(),
                StartDate = exam.StartDate,
                EndDate = exam.EndDate,
                StartedAt = now,
                ExpiresAt = now.AddSeconds(timeLimitSeconds),
                TimeLimitSeconds = timeLimitSeconds,
                TotalQuestions = sections.Sum(s => s.ExamQuestions.Count(eq => !eq.IsDeleted)),

                Sections = sections.Select(s => new PreviewSectionDto
                {
                    SectionId = s.Id,
                    SectionName = s.Category?.Name ?? "Unknown",
                    SkillType = s.Category?.Code ?? s.Category?.Name ?? "Unknown",
                    OrderIndex = s.OrderIndex,
                    Instructions = s.Instructions,

                    Questions = s.ExamQuestions
                        .Where(eq => !eq.IsDeleted)
                        .OrderBy(eq => eq.OrderIndex)
                        .Select(eq => new PreviewQuestionDto
                        {
                            ExamQuestionId = eq.Id,
                            QuestionId = eq.QuestionId,
                            OrderIndex = eq.OrderIndex,
                            Point = (double)eq.Point,
                            Content = eq.Question?.Content ?? string.Empty,
                            QuestionType = eq.Question?.QuestionType ?? QuestionTypeEnum.SingleChoice,
                            Explanation = eq.Question?.Explanation,

                            AudioUrl = eq.Question?.Media?
                                .FirstOrDefault(m => IsAudio(m.MediaType, m.Url))?.Url,
                            ImageUrl = eq.Question?.Media?
                                .FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url,

                            GroupId = eq.Question?.GroupId,
                            GroupContent = eq.Question?.Group?.Content,
                            GroupAudioUrl = eq.Question?.Group?.Media?
                                .FirstOrDefault(m => IsAudio(m.MediaType, m.Url))?.Url,
                                                GroupImageUrl = eq.Question?.Group?.Media?
                                .FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url,


                            Answers = eq.Question?.Answers
                                .OrderBy(a => a.OrderIndex)
                                .Select(a => new PreviewAnswerOption
                                {
                                    Id = a.Id,
                                    Content = a.Content ?? string.Empty,
                                    OrderIndex = a.OrderIndex,
                                    IsCorrect = request.ShowCorrectAnswers && a.IsCorrect,
                                })
                                .ToList() ?? new(),
                        })
                        .ToList(),
                }).ToList(),
            };
        }

        private static bool IsAudio(string? t, string? u) => ResolveMediaType(t, u) == "audio";
        private static bool IsImage(string? t, string? u) => ResolveMediaType(t, u) == "image";

        private static string ResolveMediaType(string? mediaType, string? url) =>
            !string.IsNullOrWhiteSpace(mediaType) ? mediaType.ToLower() : GetTypeFromUrl(url);

        private static string GetTypeFromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "unknown";
            var u = url.ToLower();
            if (u.EndsWith(".mp3") || u.EndsWith(".wav") || u.EndsWith(".ogg") || u.EndsWith(".m4a")) return "audio";
            if (u.EndsWith(".jpg") || u.EndsWith(".jpeg") || u.EndsWith(".png") || u.EndsWith(".webp")) return "image";
            if (u.Contains("/video/upload/")) return "audio"; // Cloudinary audio
            if (u.Contains("/image/upload/")) return "image";
            return "unknown";
        }
    }
}