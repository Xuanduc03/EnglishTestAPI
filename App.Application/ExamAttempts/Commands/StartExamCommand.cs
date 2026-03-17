using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.ExamAttempts.Commands
{
    public class StartExamCommand : IRequest<StartExamResult>
    {
        [Required] public Guid ExamId { get; set; }
        [Required] public Guid UserId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    // ── Result DTO ───────────────────────────────────────────────
    public class StartExamResult
    {
        public Guid AttemptId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int TotalQuestions { get; set; }
        public List<ExamSectionPreview> Sections { get; set; } = new();
    }

    public class ExamSectionPreview
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string SkillType { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string? Instructions { get; set; }
        public List<ExamQuestionPreview> Questions { get; set; } = new();
    }

    public class ExamQuestionPreview
    {
        public Guid ExamQuestionId { get; set; }
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }   // 0-based global
        public double Point { get; set; }

        // ✅ null với Part 3/4 (ẩn transcript) — FE dựa vào groupContent
        public string? Content { get; set; }
        public QuestionTypeEnum QuestionType { get; set; } = QuestionTypeEnum.SingleChoice;

        // Media câu đơn
        public bool HasAudio { get; set; }
        public bool HasImage { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }

        // Group info
        public Guid? GroupId { get; set; }
        // ✅ null với Part 3/4 (ẩn transcript hội thoại)
        public string? GroupContent { get; set; }
        public string? GroupAudioUrl { get; set; }
        public string? GroupImageUrl { get; set; }

        public List<AnswerOption> Answers { get; set; } = new();
    }

    public class AnswerOption
    {
        public Guid Id { get; set; }
        // ✅ null với Part 1/2 (ẩn nội dung đáp án, chỉ phát audio)
        public string? Content { get; set; }
        public int OrderIndex { get; set; }
    }

    // ── Handler ──────────────────────────────────────────────────
    public class StartExamCommandHandler : IRequestHandler<StartExamCommand, StartExamResult>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;
        private const int MaxQuestionsPerExam = 500;
        private const int MaxDurationMinutes = 480;

        public StartExamCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<StartExamResult> Handle(
            StartExamCommand request,
            CancellationToken cancellationToken)
        {
            // ── 0. Auth ───────────────────────────────────────────
            var loggedInUserId = _currentUserService.UserId;
            Guid currentUserId;

            if (request.UserId == Guid.Empty)
            {
                currentUserId = Guid.NewGuid(); // guest
            }
            else
            {
                if (loggedInUserId == Guid.Empty)
                    throw new UnauthorizedAccessException("Người dùng chưa xác thực.");

                if (request.UserId != loggedInUserId)
                    throw new UnauthorizedAccessException("Không thể bắt đầu bài thi cho người dùng khác.");

                currentUserId = request.UserId;
            }

            // ── 1. Load & validate exam ───────────────────────────
            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Id == request.ExamId && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy bài thi {request.ExamId}");

            ValidateExamStatus(exam);

            // ── 2. Check active attempt ───────────────────────────
            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                var activeAttempt = await _context.ExamAttempts
                    .FirstOrDefaultAsync(a =>
                        a.UserId == currentUserId &&
                        a.ExamId == request.ExamId &&
                        a.Status == ExamAttemptStatus.InProgress &&
                        a.ExpiresAt > DateTime.UtcNow,
                        cancellationToken);

                if (activeAttempt != null)
                    throw new InvalidOperationException(
                        $"Đã tồn tại phiên thi đang làm: {activeAttempt.Id}");

                // ── 3. Create attempt ─────────────────────────────
                var now = DateTime.UtcNow;
                var timeLimitSeconds = CalculateTimeLimitSeconds(exam.Duration);

                var attempt = new ExamAttempt
                {
                    Id = Guid.NewGuid(),
                    UserId = currentUserId,
                    ExamId = exam.Id,
                    StartedAt = now,
                    ExpiresAt = now.AddSeconds(timeLimitSeconds),
                    TimeLimitSeconds = timeLimitSeconds,
                    Status = ExamAttemptStatus.InProgress,
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                // ── 4. Load sections + questions ──────────────────
                var sections = await LoadSectionsWithQuestions(request.ExamId, cancellationToken);
                var allQuestions = sections.SelectMany(s => s.Questions).ToList();

                if (!allQuestions.Any())
                    throw new InvalidOperationException("Bài thi không có câu hỏi");

                if (allQuestions.Count > MaxQuestionsPerExam)
                    throw new InvalidOperationException(
                        $"Bài thi vượt quá giới hạn {MaxQuestionsPerExam} câu");

                attempt.TotalQuestions = allQuestions.Count;

                // ── 5. Create exam answers ────────────────────────
                var examAnswers = allQuestions.Select(eq => new ExamAnswer
                {
                    Id = Guid.NewGuid(),
                    ExamAttemptId = attempt.Id,
                    ExamQuestionId = eq.Id,
                    QuestionId = eq.QuestionId,
                    IsAnswered = false,
                    IsCorrect = false,
                    Point = 0,
                    VersionNumber = 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToList();

                // ── 6. Save ───────────────────────────────────────
                _context.ExamAttempts.Add(attempt);
                _context.ExamAnswers.AddRange(examAnswers);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // ── 7. Build response ─────────────────────────────
                return BuildResult(attempt, sections);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // ── Load sections với questions ───────────────────────────
        private async Task<List<SectionWithQuestions>> LoadSectionsWithQuestions(
            Guid examId, CancellationToken ct)
        {
            var sections = await _context.ExamSections
                .AsNoTracking()
                .Where(s => s.ExamId == examId && !s.IsDeleted)
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
                .ToListAsync(ct);

            return sections.Select(s => new SectionWithQuestions
            {
                Section = s,
                Questions = s.ExamQuestions
                    .Where(eq => !eq.IsDeleted)
                    .OrderBy(eq => eq.OrderIndex)
                    .ToList()
            }).ToList();
        }

        // ── Build response ────────────────────────────────────────
        private StartExamResult BuildResult(
            ExamAttempt attempt,
            List<SectionWithQuestions> sections)
        {
            int globalIndex = 0;

            return new StartExamResult
            {
                AttemptId = attempt.Id,
                StartedAt = attempt.StartedAt,
                ExpiresAt = attempt.ExpiresAt,
                TimeLimitSeconds = attempt.TimeLimitSeconds,
                TotalQuestions = attempt.TotalQuestions,

                Sections = sections.Select(sw => new ExamSectionPreview
                {
                    SectionId = sw.Section.Id,
                    SectionName = sw.Section.Category?.Name ?? "Unknown",
                    SkillType = sw.Section.Category?.Code ?? "",
                    OrderIndex = sw.Section.OrderIndex,
                    Instructions = sw.Section.Instructions,

                    Questions = sw.Questions.Select(eq =>
                    {
                        var q = eq.Question;

                        // Detect loại part dựa vào media của group/câu
                        var hasGroupAudio = q.Group?.Media?.Any(m => IsAudio(m.MediaType, m.Url)) ?? false;
                        var hasOwnAudio = q.Media?.Any(m => IsAudio(m.MediaType, m.Url)) ?? false;
                        var isListeningGroup = q.GroupId.HasValue && hasGroupAudio; // Part 3, 4
                        var isListeningSingle = !q.GroupId.HasValue && hasOwnAudio;  // Part 1, 2

                        return new ExamQuestionPreview
                        {
                            ExamQuestionId = eq.Id,
                            QuestionId = eq.QuestionId,
                            OrderIndex = globalIndex++,  
                            Point = (double)eq.Point,
                            QuestionType = q.QuestionType,

                            Content = q.Content,

                            // Media câu đơn
                            HasAudio = hasOwnAudio,
                            HasImage = q.Media?.Any(m => IsImage(m.MediaType, m.Url)) ?? false,
                            AudioUrl = hasOwnAudio
                                ? q.Media!.First(m => IsAudio(m.MediaType, m.Url)).Url
                                : null,
                            ImageUrl = q.Media?.FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url,

                            // Group info
                            GroupId = q.GroupId,
                            GroupContent = isListeningGroup ? null : q.Group?.Content,
                            GroupAudioUrl = q.Group?.Media?
                                .FirstOrDefault(m => IsAudio(m.MediaType, m.Url))?.Url,
                            GroupImageUrl = q.Group?.Media?
                                .FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url,

                            Answers = q.Answers
                                .OrderBy(a => a.OrderIndex)
                                .Select(a => new AnswerOption
                                {
                                    Id = a.Id,
                                    Content = isListeningSingle ? null : a.Content,
                                    OrderIndex = a.OrderIndex,
                                })
                                .ToList(),
                        };
                    }).ToList()
                }).ToList()
            };
        }

        // ── Helpers ───────────────────────────────────────────────
        private void ValidateExamStatus(Exam exam)
        {
            if (exam.Status != ExamStatus.Published)
                throw new InvalidOperationException($"Bài thi chưa được xuất bản ({exam.Status})");

            var now = DateTime.UtcNow;
            if (exam.StartDate.HasValue && exam.StartDate > now)
                throw new InvalidOperationException($"Bài thi chưa mở: {exam.StartDate}");
            if (exam.EndDate.HasValue && exam.EndDate < now)
                throw new InvalidOperationException($"Bài thi đã kết thúc: {exam.EndDate}");
            if (exam.Duration <= 0 || exam.Duration > MaxDurationMinutes)
                throw new InvalidOperationException($"Thời lượng không hợp lệ: {exam.Duration} phút");
            if (string.IsNullOrEmpty(exam.Code))
                throw new InvalidOperationException("Bài thi chưa có mã đề");
        }

        private int CalculateTimeLimitSeconds(int durationMinutes)
        {
            if (durationMinutes <= 0 || durationMinutes > MaxDurationMinutes)
                throw new ArgumentOutOfRangeException(nameof(durationMinutes));
            return durationMinutes * 60;
        }

        private static bool IsAudio(string? t, string? u) => ResolveMediaType(t, u) == "audio";
        private static bool IsImage(string? t, string? u) => ResolveMediaType(t, u) == "image";

        private static string ResolveMediaType(string? mediaType, string? url) =>
            !string.IsNullOrWhiteSpace(mediaType)
                ? mediaType.ToLower()
                : GetTypeFromUrl(url);

        private static string GetTypeFromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return "unknown";
            var u = url.ToLower();
            if (u.EndsWith(".mp3") || u.EndsWith(".wav") || u.EndsWith(".ogg") || u.EndsWith(".m4a")) return "audio";
            if (u.EndsWith(".jpg") || u.EndsWith(".jpeg") || u.EndsWith(".png") || u.EndsWith(".webp")) return "image";
            if (u.Contains("/video/upload/")) return "audio";
            if (u.Contains("/image/upload/")) return "image";
            return "unknown";
        }

        private class SectionWithQuestions
        {
            public ExamSection Section { get; set; }
            public List<ExamQuestion> Questions { get; set; } = new();
        }
    }
}