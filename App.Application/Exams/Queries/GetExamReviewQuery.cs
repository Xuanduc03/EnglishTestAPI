using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace App.Application.Exams.Queries
{
    // ============================================================
    // QUERY: GET EXAM PREVIEW
    // GET /api/admin/exams/{examId}/preview
    // ============================================================
    public class GetExamPreviewQuery : IRequest<ExamPreviewDto>
    {
        public Guid ExamId { get; set; }
        public bool ShowCorrectAnswers { get; set; } = true; // Admin mặc định thấy đáp án
    }

    // ── Handler ─────────────────────────────────────────────────
    public class GetExamPreviewQueryHandler
        : IRequestHandler<GetExamPreviewQuery, ExamPreviewDto>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _userService;

        public GetExamPreviewQueryHandler(
            IAppDbContext context,
            ICurrentUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<ExamPreviewDto> Handle(
            GetExamPreviewQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Load exam — Admin được xem cả Draft
            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Id == request.ExamId && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException($"Exam {request.ExamId} not found");

            // 2. Auth: chỉ Admin/Creator được preview
            // TODO: inject IAuthorizationService nếu dùng policy-based auth
            // await _authService.AuthorizeAsync(user, exam, "CanPreviewExam");

            // 3. Load sections + questions + answers — 1 round trip
            var sections = await _context.ExamSections
                .AsNoTracking()
                .Where(s => s.ExamId == request.ExamId && !s.IsDeleted)
                .Include(s => s.Category)
                .Include(s => s.ExamQuestions.Where(eq => !eq.IsDeleted))
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers.OrderBy(a => a.OrderIndex))
                .Include(s => s.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Media)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync(cancellationToken);

            // 4. Build DTO — KHÔNG gọi SaveChanges, KHÔNG tạo attempt
            var now = DateTime.UtcNow;
            var timeLimitSeconds = exam.Duration * 60;

            return new ExamPreviewDto
            {
                AttemptId = Guid.Empty,           // sentinel = preview mode
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
                TotalQuestions = sections.Sum(s => s.ExamQuestions.Count),

                Sections = sections.Select(s => new PreviewSectionDto
                {
                    SectionId = s.Id,
                    SectionName = s.Category?.Name ?? "Unknown",
                    SkillType = s.Category?.Name ?? "Unknown",
                    OrderIndex = s.OrderIndex,
                    Instructions = s.Instructions,
                    Questions = s.ExamQuestions
                        .OrderBy(eq => eq.OrderIndex)
                        .Select(eq => new PreviewQuestionDto
                        {
                            ExamQuestionId = eq.Id,
                            QuestionId = eq.QuestionId,
                            OrderIndex = eq.OrderIndex,
                            Point = (double)eq.Point,
                            Content = eq.Question?.Content ?? string.Empty,
                            QuestionType = eq.Question?.QuestionType ?? string.Empty,
                            AudioUrl = eq.Question?.Media?
                                .FirstOrDefault(m => m.MediaType == "audio")?.Url,
                            ImageUrl = eq.Question?.Media?
                                .FirstOrDefault(m => m.MediaType == "image")?.Url,
                            Explanation = eq.Question?.Explanation,
                            //ExplanationVi = eq.Question?.ExplanationVi,

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
    }
}
