using App.Application.Interfaces;
using App.Domain.Entities;
using global::App.Application.ExamAttempts.Commands.IELTS;
using global::App.Application.Interfaces;
using global::App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace App.Application.ExamAttempts.Queries.IELTS
{ 
    // ─────────────────────────────────────────────────────────────
    // QUERY
    // GET /api/ielts-attempts/{attemptId}/review
    // ─────────────────────────────────────────────────────────────
    public class IeltsReviewExamQuery : IRequest<IeltsReviewExamResult>
    {
        [Required] public Guid AttemptId { get; set; }
        [Required] public Guid? UserId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // RESULT DTOs
    // ─────────────────────────────────────────────────────────────
    public class IeltsReviewExamResult
    {
        public Guid AttemptId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int DurationSeconds { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int SkippedAnswers { get; set; }

        // Band scores
        public IeltsReviewBandScore Listening { get; set; } = new();
        public IeltsReviewBandScore Reading { get; set; } = new();

        // Chi tiết từng section → group → câu hỏi
        public List<IeltsReviewSection> Sections { get; set; } = new();
    }

    public class IeltsReviewBandScore
    {
        public int CorrectCount { get; set; }
        public int TotalCount { get; set; }
        public double BandScore { get; set; }
    }

    public class IeltsReviewSection
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string SkillType { get; set; } = string.Empty; // "Listening" | "Reading"
        public int Total { get; set; }
        public int Correct { get; set; }
        public int Wrong { get; set; }
        public int Skipped { get; set; }
        public List<IeltsReviewGroup> Groups { get; set; } = new();
    }

    public class IeltsReviewGroup
    {
        public Guid GroupId { get; set; }

        // Listening
        public string? AudioUrl { get; set; }
        public string? Transcript { get; set; } // expose khi review

        // Reading
        public string? PassageHtml { get; set; }
        public string? ImageUrl { get; set; }

        public List<IeltsReviewQuestion> Questions { get; set; } = new();
    }

    public class IeltsReviewQuestion
    {
        public Guid ExamQuestionId { get; set; }
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public double Point { get; set; }
        public double EarnedPoint { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsAnswered { get; set; }
        public int MaxWords { get; set; }

        public string? Content { get; set; }
        public IeltsQuestionType QuestionType { get; set; }

        // Bài làm của user
        public string? UserTextAnswer { get; set; } // fill-in
        public Guid? UserSelectedId { get; set; } // single choice
        public List<string> UserSelectedIds { get; set; } = new(); // multi choice

        // Đáp án đúng
        public List<IeltsCorrectAnswer> CorrectAnswers { get; set; } = new();

        // Options (MCQ / T-F-NG / Matching)
        public List<IeltsReviewOption> Options { get; set; } = new();

        // Giải thích (nếu có trong DB)
        public string? Explanation { get; set; }
    }

    public class IeltsCorrectAnswer
    {
        public Guid? Id { get; set; } // null với fill-in (so sánh text)
        public string? Content { get; set; } // text đáp án đúng
    }

    public class IeltsReviewOption
    {
        public Guid Id { get; set; }
        public string? Content { get; set; }
        public int OrderIndex { get; set; }
        public bool IsCorrect { get; set; } // highlight đáp án đúng
    }

    // ─────────────────────────────────────────────────────────────
    // HANDLER
    // ─────────────────────────────────────────────────────────────
    public class IeltsReviewExamQueryHandler
        : IRequestHandler<IeltsReviewExamQuery, IeltsReviewExamResult>
    {
        private readonly IAppDbContext _context;

        public IeltsReviewExamQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<IeltsReviewExamResult> Handle(
            IeltsReviewExamQuery request,
            CancellationToken ct)
        {
            // ── 1. Load attempt 
            var attempt = await _context.ExamAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiên thi {request.AttemptId}");

            // ── 2. Auth check 
            if (attempt.UserId != request.UserId)
                throw new UnauthorizedAccessException("Không thể xem bài thi của người khác");

            // ── 3. Chỉ review được bài đã nộp =
            if (attempt.Status != ExamAttemptStatus.Submitted)
                throw new InvalidOperationException("Bài thi chưa được nộp, không thể review");

            // ── 4. Load ExamAnswers + toàn bộ dữ liệu liên quan ──
            var examAnswers = await _context.ExamAnswers
                .AsNoTracking()
                .Where(a => a.ExamAttemptId == request.AttemptId)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.ExamSection)
                        .ThenInclude(s => s.Category)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers.OrderBy(ans => ans.OrderIndex))
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Group)
                            .ThenInclude(g => g.Media)
                .ToListAsync(ct);

            if (!examAnswers.Any())
                throw new InvalidOperationException("Không tìm thấy câu hỏi trong bài thi");

            // ── 5. Query SectionId → SkillType thẳng từ DB ───────
            //
            // Dùng cùng pattern đã fix ở SubmitExam:
            // ExamSection.CategoryId → Categories(Part).Code → detect _L_ / _R_
            //
            var sectionIds = examAnswers
                .Select(a => a.ExamQuestions.ExamSectionId)
                .Distinct()
                .ToList();

            var sectionSkillMap = await _context.ExamSections
                .AsNoTracking()
                .Where(es => sectionIds.Contains(es.Id))
                .Join(
                    _context.Categories,
                    es => es.CategoryId,
                    c => c.Id,
                    (es, c) => new
                    {
                        SectionId = es.Id,
                        CategoryCode = c.Code ?? "",
                    }
                )
                .ToDictionaryAsync(x => x.SectionId, x => x.CategoryCode, ct);

            // ── 6. Group theo Section → Group → Question ──────────
            var sectionGroups = examAnswers
                .GroupBy(a => new
                {
                    SectionId = a.ExamQuestions.ExamSectionId,
                    SectionName = a.ExamQuestions.ExamSection?.Category?.Name ?? "Unknown",
                    CategoryCode = sectionSkillMap.TryGetValue(
                        a.ExamQuestions.ExamSectionId, out var code) ? code : "",
                })
                .OrderBy(g => g.Key.CategoryCode)
                .ToList();

            // ── 7. Tính tổng Listening / Reading correct ──────────
            int listeningCorrect = 0, listeningTotal = 0;
            int readingCorrect = 0, readingTotal = 0;

            foreach (var sg in sectionGroups)
            {
                var upper = sg.Key.CategoryCode.ToUpperInvariant();
                if (upper.Contains("_L_"))
                {
                    listeningCorrect += sg.Count(a => a.IsCorrect);
                    listeningTotal += sg.Count();
                }
                else if (upper.Contains("_R_"))
                {
                    readingCorrect += sg.Count(a => a.IsCorrect);
                    readingTotal += sg.Count();
                }
            }

            // ── 8. Band scores ────────────────────────────────────
            var listeningBand = ListeningBand(listeningCorrect);
            var readingBand = ReadingBand(readingCorrect);

            // ── 9. Build Sections ─────────────────────────────────
            int globalIndex = 1;

            var reviewSections = sectionGroups.Select(sg =>
            {
                var skillType = ResolveSkillType(sg.Key.CategoryCode);

                // Group answers theo GroupId của Question
                var groupedAnswers = sg
                    .OrderBy(a => a.ExamQuestions.OrderIndex)
                    .GroupBy(a =>
                        a.ExamQuestions.Question?.GroupId
                        ?? a.ExamQuestions.QuestionId)
                    .ToList();

                var reviewGroups = groupedAnswers.Select(grp =>
                {
                    var firstQ = grp.First().ExamQuestions.Question;
                    var group = firstQ?.Group;

                    var audioUrl = group?.Media?
                        .FirstOrDefault(m => IsAudio(m.MediaType, m.Url))?.Url;
                    var passageHtml = group?.Content;
                    var imageUrl = group?.Media?
                        .FirstOrDefault(m => IsImage(m.MediaType, m.Url))?.Url;
                    var transcript = group?.Transcript; // expose khi review

                    var reviewQuestions = grp.Select(ea =>
                    {
                        var eq = ea.ExamQuestions;
                        var q = eq.Question;
                        var qType = MapQuestionType(q.QuestionType);

                        // ── Bài làm của user ─────────────────────
                        string? userText = null;
                        Guid? userSingleId = null;
                        List<string> userMultiIds = new();

                        if (IsFillIn(qType))
                        {
                            userText = ea.TextAnswer;
                        }
                        else if (qType == IeltsQuestionType.MultipleChoice)
                        {
                            // TextAnswer lưu JSON ["id1","id2"]
                            if (!string.IsNullOrWhiteSpace(ea.TextAnswer))
                            {
                                try
                                {
                                    userMultiIds = JsonSerializer
                                        .Deserialize<List<string>>(ea.TextAnswer) ?? new();
                                }
                                catch { /* bỏ qua nếu JSON lỗi */ }
                            }
                        }
                        else
                        {
                            userSingleId = ea.SelectedAnswerId;
                        }

                        // ── Đáp án đúng ───────────────────────────
                        var correctAnswers = IsFillIn(qType)
                            ? q.Answers
                                .Where(a => a.IsCorrect)
                                .Select(a => new IeltsCorrectAnswer
                                {
                                    Id = null,        // fill-in không cần Id
                                    Content = a.Content,
                                })
                                .ToList()
                            : q.Answers
                                .Where(a => a.IsCorrect)
                                .Select(a => new IeltsCorrectAnswer
                                {
                                    Id = a.Id,
                                    Content = a.Content,
                                })
                                .ToList();

                        // ── Options với IsCorrect highlight ───────
                        var options = q.Answers
                            .OrderBy(a => a.OrderIndex)
                            .Select(a => new IeltsReviewOption
                            {
                                Id = a.Id,
                                Content = a.Content,
                                OrderIndex = a.OrderIndex,
                                IsCorrect = a.IsCorrect,
                            })
                            .ToList();

                        return new IeltsReviewQuestion
                        {
                            ExamQuestionId = eq.Id,
                            QuestionId = eq.QuestionId,
                            OrderIndex = globalIndex++,
                            Point = (double)eq.Point,
                            EarnedPoint = ea.Point,
                            IsCorrect = ea.IsCorrect,
                            IsAnswered = ea.IsAnswered,
                            MaxWords = q.MaxWords ?? 3,
                            Content = q.Content,
                            QuestionType = qType,
                            UserTextAnswer = userText,
                            UserSelectedId = userSingleId,
                            UserSelectedIds = userMultiIds,
                            CorrectAnswers = correctAnswers,
                            Options = options,
                            Explanation = q.Explanation,
                        };
                    }).ToList();

                    return new IeltsReviewGroup
                    {
                        GroupId = group?.Id ?? firstQ?.Id ?? Guid.Empty,
                        AudioUrl = audioUrl,
                        Transcript = transcript,
                        PassageHtml = passageHtml,
                        ImageUrl = imageUrl,
                        Questions = reviewQuestions,
                    };
                }).ToList();

                return new IeltsReviewSection
                {
                    SectionId = sg.Key.SectionId,
                    SectionName = sg.Key.SectionName,
                    SkillType = skillType,
                    Total = sg.Count(),
                    Correct = sg.Count(a => a.IsCorrect),
                    Wrong = sg.Count(a => a.IsAnswered && !a.IsCorrect),
                    Skipped = sg.Count(a => !a.IsAnswered),
                    Groups = reviewGroups,
                };
            }).ToList();

            // ── 10. Build result ──────────────────────────────────
            var totalCorrect = examAnswers.Count(a => a.IsCorrect);
            var totalWrong = examAnswers.Count(a => a.IsAnswered && !a.IsCorrect);
            var totalSkipped = examAnswers.Count(a => !a.IsAnswered);

            return new IeltsReviewExamResult
            {
                AttemptId = attempt.Id,
                SubmittedAt = attempt.SubmitedAt ?? DateTime.UtcNow,
                TotalQuestions = examAnswers.Count,
                CorrectAnswers = totalCorrect,
                WrongAnswers = totalWrong,
                SkippedAnswers = totalSkipped,
                Listening = new IeltsReviewBandScore
                {
                    CorrectCount = listeningCorrect,
                    TotalCount = listeningTotal,
                    BandScore = listeningBand,
                },
                Reading = new IeltsReviewBandScore
                {
                    CorrectCount = readingCorrect,
                    TotalCount = readingTotal,
                    BandScore = readingBand,
                },
                Sections = reviewSections,
            };
        }

        // ─────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────

        private static string ResolveSkillType(string code)
        {
            var upper = code.ToUpperInvariant();
            if (upper.Contains("_L_")) return "Listening";
            if (upper.Contains("_R_")) return "Reading";
            return "Unknown";
        }

        private static bool IsFillIn(IeltsQuestionType t) => t switch
        {
            IeltsQuestionType.FormCompletion => true,
            IeltsQuestionType.NoteCompletion => true,
            IeltsQuestionType.SentenceCompletion => true,
            IeltsQuestionType.ShortAnswer => true,
            IeltsQuestionType.MapLabeling => true,
            _ => false,
        };

        private static IeltsQuestionType MapQuestionType(QuestionTypeEnum t) => t switch
        {
            QuestionTypeEnum.FormCompletion => IeltsQuestionType.FormCompletion,
            QuestionTypeEnum.NoteCompletion => IeltsQuestionType.NoteCompletion,
            QuestionTypeEnum.SentenceCompletion => IeltsQuestionType.SentenceCompletion,
            QuestionTypeEnum.ShortAnswer => IeltsQuestionType.ShortAnswer,
            QuestionTypeEnum.MapLabeling => IeltsQuestionType.MapLabeling,
            QuestionTypeEnum.SingleChoice => IeltsQuestionType.SingleChoice,
            QuestionTypeEnum.MultipleChoice => IeltsQuestionType.MultipleChoice,
            QuestionTypeEnum.TrueFalseNotGiven => IeltsQuestionType.TrueFalseNotGiven,
            QuestionTypeEnum.YesNoNotGiven => IeltsQuestionType.YesNoNotGiven,
            QuestionTypeEnum.MatchingHeading => IeltsQuestionType.MatchingHeading,
            QuestionTypeEnum.MatchingInformation => IeltsQuestionType.MatchingInformation,
            QuestionTypeEnum.Matching => IeltsQuestionType.Matching,
            _ => IeltsQuestionType.ShortAnswer,
        };

        private static bool IsAudio(string? t, string? u) =>
            !string.IsNullOrWhiteSpace(t)
                ? t.ToLower() == "audio"
                : !string.IsNullOrEmpty(u) &&
                  (u.EndsWith(".mp3") || u.EndsWith(".wav") ||
                   u.EndsWith(".ogg") || u.EndsWith(".m4a") ||
                   u.Contains("/video/upload/"));

        private static bool IsImage(string? t, string? u) =>
            !string.IsNullOrWhiteSpace(t)
                ? t.ToLower() == "image"
                : !string.IsNullOrEmpty(u) &&
                  (u.EndsWith(".jpg") || u.EndsWith(".jpeg") ||
                   u.EndsWith(".png") || u.EndsWith(".webp") ||
                   u.Contains("/image/upload/"));

        // Cambridge standard band score tables
        private static double ListeningBand(int c) => c switch
        {
            >= 39 => 9.0,
            >= 37 => 8.5,
            >= 35 => 8.0,
            >= 32 => 7.5,
            >= 30 => 7.0,
            >= 26 => 6.5,
            >= 23 => 6.0,
            >= 18 => 5.5,
            >= 16 => 5.0,
            >= 13 => 4.5,
            >= 11 => 4.0,
            >= 8 => 3.5,
            >= 6 => 3.0,
            >= 4 => 2.5,
            >= 3 => 2.0,
            >= 2 => 1.5,
            >= 1 => 1.0,
            _ => 0.0,
        };

        private static double ReadingBand(int c) => c switch
        {
            >= 39 => 9.0,
            >= 37 => 8.5,
            >= 35 => 8.0,
            >= 33 => 7.5,
            >= 30 => 7.0,
            >= 27 => 6.5,
            >= 23 => 6.0,
            >= 19 => 5.5,
            >= 15 => 5.0,
            >= 13 => 4.5,
            >= 10 => 4.0,
            >= 8 => 3.5,
            >= 6 => 3.0,
            >= 4 => 2.5,
            >= 3 => 2.0,
            >= 2 => 1.5,
            >= 1 => 1.0,
            _ => 0.0,
        };
    }
}