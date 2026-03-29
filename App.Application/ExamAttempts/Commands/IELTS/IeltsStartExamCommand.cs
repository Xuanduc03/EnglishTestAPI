using App.Application.Interfaces;
using App.Application.Services.Interface;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.ExamAttempts.Commands.IELTS
{
    // 
    // COMMAND
    public class IeltsStartExamCommand : IRequest<IeltsStartExamResult>
    {
        [Required] public Guid ExamId { get; set; }
        [Required] public Guid UserId { get; set; }
    }

    // RESULT DTOs
    public class IeltsStartExamResult
    {
        public Guid AttemptId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int TotalQuestions { get; set; }
        public List<IeltsSectionPreview> Sections { get; set; } = new();
    }

    public class IeltsSectionPreview
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string SkillType { get; set; } = string.Empty;   // "Listening" | "Reading"
        public int OrderIndex { get; set; }
        public string? Instructions { get; set; }
        public List<IeltsGroupPreview> Groups { get; set; } = new();
    }

    /// <summary>
    /// Đại diện cho 1 QuestionGroup (đoạn nghe / đoạn đọc).
    /// Listening  → AudioUrl có giá trị, PassageHtml = null
    /// Reading    → PassageHtml có giá trị, AudioUrl  = null
    /// </summary>
    public class IeltsGroupPreview
    {
        public Guid GroupId { get; set; }

        // Listening
        public string? AudioUrl { get; set; }   // mp3 từ Group.Media
        public string? Transcript { get; set; }   // hiện sau khi nộp bài

        // Reading
        public string? PassageHtml { get; set; }   // Group.Content (HTML)
        public string? ImageUrl { get; set; }   // ảnh đính kèm đoạn đọc (nếu có)

        public List<IeltsQuestionPreview> Questions { get; set; } = new();
    }

    public class IeltsQuestionPreview
    {
        public Guid ExamQuestionId { get; set; }
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }   // số thứ tự global (1–80)
        public double Point { get; set; }
        public int MaxWords { get; set; }   // FormCompletion: số từ tối đa

        // Nội dung câu hỏi  e.g. "Role: 1…………………"
        public string? Content { get; set; }

        public IeltsQuestionType QuestionType { get; set; }

        // Chỉ có giá trị với MCQ / TrueFalseNotGiven / YesNoNotGiven / Matching
        public List<IeltsAnswerOption> Options { get; set; } = new();
    }

    /// <summary>
    /// Sub-enum gộp các dạng IELTS để frontend không cần switch theo QuestionTypeEnum gốc.
    /// </summary>
    public enum IeltsQuestionType
    {
        // TOEIC
        SingleChoice = 1,
        MultipleChoice = 2,
        FillBlank = 3,

        // IELTS Matching
        Matching = 4,
        MatchingHeading = 5,
        MatchingInformation = 6,
        MatchingSentenceEnds = 7,

        // True/False
        TrueFalseNotGiven = 8,
        YesNoNotGiven = 9,

        // Completion
        ShortAnswer = 10,
        NoteCompletion = 11,
        FormCompletion = 12,
        TableCompletion = 13,
        SummaryCompletion = 14,
        SentenceCompletion = 15,
        MapLabeling = 16,
    }

    public class IeltsAnswerOption
    {
        public Guid Id { get; set; }
        public string? Content { get; set; }
        public int OrderIndex { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // HANDLER
    // ─────────────────────────────────────────────────────────────
    public class IeltsStartExamCommandHandler
        : IRequestHandler<IeltsStartExamCommand, IeltsStartExamResult>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        private const int MaxDurationMinutes = 480;

        public IeltsStartExamCommandHandler(
            IAppDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        public async Task<IeltsStartExamResult> Handle(
            IeltsStartExamCommand command,
            CancellationToken ct)
        {
            // ── 1. Auth ──────────────────────────────────────────
            var currentUserId = ResolveUserId(command.UserId);

            // ── 2. Load & validate exam ──────────────────────────
            var exam = await _context.Exams
                .AsNoTracking()
                .Where(e => e.Id == command.ExamId && !e.IsDeleted)
                .FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy bài thi {command.ExamId}");

            ValidateExam(exam);

            // ── 3. Không cho vào nếu đang có phiên dở ────────────
            using var tx = await _context.BeginTransactionAsync(ct);
            try
            {
                var active = await _context.ExamAttempts
                    .FirstOrDefaultAsync(a =>
                        a.UserId == currentUserId &&
                        a.ExamId == command.ExamId &&
                        a.Status == ExamAttemptStatus.InProgress &&
                        a.ExpiresAt > DateTime.UtcNow, ct);

                if (active != null)
                    throw new InvalidOperationException(
                        $"Đang có phiên thi chưa hoàn thành: {active.Id}");

                // ── 4. Tạo attempt ────────────────────────────────
                var now = DateTime.UtcNow;
                var timeLimitSeconds = exam.Duration * 60;

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

                // ── 5. Load sections → groups → questions ─────────
                var sections = await LoadSections(command.ExamId, ct);
                var allExamQuestions = sections
                    .SelectMany(s => s.ExamQuestions)
                    .ToList();

                if (!allExamQuestions.Any())
                    throw new InvalidOperationException("Bài thi chưa có câu hỏi");

                attempt.TotalQuestions = allExamQuestions.Count;

                // ── 6. Tạo ExamAnswer (slot trả lời) ──────────────
                var answers = allExamQuestions.Select(eq => new ExamAnswer
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

                _context.ExamAttempts.Add(attempt);
                _context.ExamAnswers.AddRange(answers);
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // ── 7. Build response ─────────────────────────────
                return BuildResult(attempt, sections);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────
        // LOAD
        // ─────────────────────────────────────────────────────────
        private async Task<List<ExamSection>> LoadSections(Guid examId, CancellationToken ct)
        {
            return await _context.ExamSections
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
        }

        // ─────────────────────────────────────────────────────────
        // BUILD RESULT
        // ─────────────────────────────────────────────────────────
        private static IeltsStartExamResult BuildResult(
            ExamAttempt attempt,
            List<ExamSection> sections)
        {
            int globalIndex = 1; // câu số 1 → 80

            var sectionPreviews = sections.Select(section =>
            {
                var skillType = ResolveSkillType(section.Category?.Code);

                // Gom câu hỏi theo GroupId
                // Mỗi Group = 1 đoạn nghe hoặc 1 passage đọc
                var groupedQuestions = section.ExamQuestions
                    .Where(eq => !eq.IsDeleted)
                    .OrderBy(eq => eq.OrderIndex)
                    .GroupBy(eq => eq.Question.GroupId ?? eq.Question.Id) // câu không có group → tự thành 1 group
                    .ToList();

                var groups = groupedQuestions.Select(grp =>
                {
                    var firstQ = grp.First().Question;
                    var group = firstQ.Group; // null nếu câu đơn không có group

                    // ── Audio (Listening) ──────────────────────────
                    // Audio luôn nằm ở Group.Media, không bao giờ ở Question.Media
                    var audioUrl = group?.Media?
                        .FirstOrDefault(m => IsAudio(m.MediaType, m.Url))?.Url;

                    // ── Passage (Reading) ──────────────────────────
                    // Group.Content là HTML bài đọc
                    // Listening thì Group.Content = null → trả về null đúng
                    var passageHtml = group?.Content;

                    // ── Image đính kèm passage (Reading có map/diagram) ──
                    var imageUrl = group?.Media?
                        .FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url;

                    var questions = grp.Select(eq =>
                    {
                        var q = eq.Question;

                        return new IeltsQuestionPreview
                        {
                            ExamQuestionId = eq.Id,
                            QuestionId = eq.QuestionId,
                            OrderIndex = globalIndex++,
                            Point = (double)eq.Point,
                            MaxWords = q.MaxWords ?? 3,
                            Content = q.Content,
                            QuestionType = MapQuestionType(q.QuestionType),

                            // Options chỉ có ý nghĩa với MCQ / T-F-NG / Matching
                            // FormCompletion / FillBlank → Answers rỗng → Options rỗng → đúng
                            Options = q.Answers
                                .OrderBy(a => a.OrderIndex)
                                .Select(a => new IeltsAnswerOption
                                {
                                    Id = a.Id,
                                    Content = a.Content,
                                    OrderIndex = a.OrderIndex,
                                })
                                .ToList(),
                        };
                    }).ToList();

                    return new IeltsGroupPreview
                    {
                        GroupId = group?.Id ?? firstQ.Id,
                        AudioUrl = audioUrl,
                        Transcript = group?.Transcript,   // trả null khi làm bài, expose sau khi nộp
                        PassageHtml = passageHtml,
                        ImageUrl = imageUrl,
                        Questions = questions,
                    };
                }).ToList();

                return new IeltsSectionPreview
                {
                    SectionId = section.Id,
                    SectionName = section.Category?.Name ?? string.Empty,
                    SkillType = skillType,
                    OrderIndex = section.OrderIndex,
                    Instructions = section.Instructions,
                    Groups = groups,
                };
            }).ToList();

            return new IeltsStartExamResult
            {
                AttemptId = attempt.Id,
                StartedAt = attempt.StartedAt,
                ExpiresAt = attempt.ExpiresAt,
                TimeLimitSeconds = attempt.TimeLimitSeconds,
                TotalQuestions = attempt.TotalQuestions,
                Sections = sectionPreviews,
            };
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Đọc CategoryCode để biết skill.
        /// Convention DB: IELTS_L_S1 → Listening, IELTS_R_P1 → Reading
        /// </summary>
        private static string ResolveSkillType(string? categoryCode)
        {
            if (string.IsNullOrEmpty(categoryCode)) return "Unknown";
            var upper = categoryCode.ToUpperInvariant();
            if (upper.Contains("_L_")) return "Listening";
            if (upper.Contains("_R_")) return "Reading";
            return "Unknown";
        }

        private static IeltsQuestionType MapQuestionType(QuestionTypeEnum t) => t switch
        {
            QuestionTypeEnum.SingleChoice => IeltsQuestionType.SingleChoice,
            QuestionTypeEnum.MultipleChoice => IeltsQuestionType.MultipleChoice,
            QuestionTypeEnum.FillBlank => IeltsQuestionType.FillBlank,
            QuestionTypeEnum.Matching => IeltsQuestionType.Matching,
            QuestionTypeEnum.MatchingHeading => IeltsQuestionType.MatchingHeading,
            QuestionTypeEnum.MatchingInformation => IeltsQuestionType.MatchingInformation,
            QuestionTypeEnum.MatchingSentenceEnds => IeltsQuestionType.MatchingSentenceEnds,
            QuestionTypeEnum.TrueFalseNotGiven => IeltsQuestionType.TrueFalseNotGiven,
            QuestionTypeEnum.YesNoNotGiven => IeltsQuestionType.YesNoNotGiven,
            QuestionTypeEnum.ShortAnswer => IeltsQuestionType.ShortAnswer,
            QuestionTypeEnum.NoteCompletion => IeltsQuestionType.NoteCompletion,
            QuestionTypeEnum.FormCompletion => IeltsQuestionType.FormCompletion,
            QuestionTypeEnum.TableCompletion => IeltsQuestionType.TableCompletion,
            QuestionTypeEnum.SummaryCompletion => IeltsQuestionType.SummaryCompletion,
            QuestionTypeEnum.SentenceCompletion => IeltsQuestionType.SentenceCompletion,
            QuestionTypeEnum.MapLabeling => IeltsQuestionType.MapLabeling,
            _ => IeltsQuestionType.ShortAnswer,
        };

        private static bool IsAudio(string? t, string? u) =>
            !string.IsNullOrWhiteSpace(t)
                ? t.ToLower() == "audio"
                : !string.IsNullOrEmpty(u) &&
                  (u.EndsWith(".mp3") || u.EndsWith(".wav") ||
                   u.EndsWith(".ogg") || u.EndsWith(".m4a") ||
                   u.Contains("/video/upload/"));   // Cloudinary audio dùng /video/upload/

        private static bool IsImage(string? t, string? u) =>
            !string.IsNullOrWhiteSpace(t)
                ? t.ToLower() == "image"
                : !string.IsNullOrEmpty(u) &&
                  (u.EndsWith(".jpg") || u.EndsWith(".jpeg") ||
                   u.EndsWith(".png") || u.EndsWith(".webp") ||
                   u.Contains("/image/upload/"));

        private Guid ResolveUserId(Guid requestUserId)
        {
            if (requestUserId == Guid.Empty) return Guid.NewGuid(); // guest

            var loggedIn = _currentUserService.UserId;
            if (loggedIn == Guid.Empty)
                throw new UnauthorizedAccessException("Người dùng chưa xác thực.");
            if (requestUserId != loggedIn)
                throw new UnauthorizedAccessException("Không thể bắt đầu bài thi cho người dùng khác.");

            return requestUserId;
        }

        private static void ValidateExam(Exam exam)
        {
            if (exam.Status != ExamStatus.Published)
                throw new InvalidOperationException($"Bài thi chưa xuất bản ({exam.Status})");

            var now = DateTime.UtcNow;
            if (exam.StartDate.HasValue && exam.StartDate > now)
                throw new InvalidOperationException($"Bài thi chưa mở: {exam.StartDate:O}");
            if (exam.EndDate.HasValue && exam.EndDate < now)
                throw new InvalidOperationException($"Bài thi đã kết thúc: {exam.EndDate:O}");
            if (exam.Duration <= 0 || exam.Duration > 480)
                throw new InvalidOperationException($"Thời lượng không hợp lệ: {exam.Duration} phút");
            if (string.IsNullOrEmpty(exam.Code))
                throw new InvalidOperationException("Bài thi chưa có mã đề");
        }
    }
}
