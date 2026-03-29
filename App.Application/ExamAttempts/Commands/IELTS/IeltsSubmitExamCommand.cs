using App.Application.Interfaces;
using App.Application.Leaderboards.Commands;
using App.Application.Questions.Commands;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace App.Application.ExamAttempts.Commands.IELTS
{
    public class IeltsSubmitExamCommand : IRequest<IeltsSubmitExamResult>
    {
        [Required] public Guid AttemptId { get; set; }
        [Required] public Guid UserId { get; set; }
        public bool IsAutoSubmit { get; set; } = false;
    }

    public class IeltsSubmitExamResult
    {
        public Guid AttemptId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int DurationSeconds { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
        public int SkippedAnswers { get; set; }
        public IeltsBandScore Listening { get; set; } = new();
        public IeltsBandScore Reading { get; set; } = new();
        public List<IeltsSectionResult> Sections { get; set; } = new();
    }

    public class IeltsBandScore
    {
        public int CorrectCount { get; set; }
        public double BandScore { get; set; }
    }

    public class IeltsSectionResult
    {
        public string SectionName { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Correct { get; set; }
        public int Wrong { get; set; }
        public int Skipped { get; set; }
    }

    public class IeltsSubmitExamCommandHandler
        : IRequestHandler<IeltsSubmitExamCommand, IeltsSubmitExamResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;
        public IeltsSubmitExamCommandHandler(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }
        public async Task<IeltsSubmitExamResult> Handle(IeltsSubmitExamCommand request, CancellationToken ct)
        {
            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiên thi {request.AttemptId}");

            if (!request.IsAutoSubmit && attempt.UserId != request.UserId)
                throw new UnauthorizedAccessException("Không thể nộp bài của người dùng khác");
            if (attempt.Status == ExamAttemptStatus.Submitted)
                throw new InvalidOperationException("Bài thi đã nộp rồi");
            if (attempt.Status == ExamAttemptStatus.Abandoned)
                throw new InvalidOperationException("Bài thi đã bị hủy");
            if (!request.IsAutoSubmit && attempt.ExpiresAt.HasValue && attempt.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Thời gian làm bài đã hết");

            var examAnswers = await _context.ExamAnswers
                .Where(a => a.ExamAttemptId == request.AttemptId)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.ExamSection)
                        .ThenInclude(s => s.Category)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)
                .ToListAsync(ct);

            if (!examAnswers.Any())
                throw new InvalidOperationException("Không tìm thấy câu hỏi trong bài thi");

            var now = DateTime.UtcNow;
            using var tx = await _context.BeginTransactionAsync(ct);
            try
            {
                // ── Chấm điểm ─────────────────────────────────────
                foreach (var ea in examAnswers)
                {
                    var q = ea.ExamQuestions?.Question;
                    if (q == null) continue;
                    (ea.IsCorrect, ea.Point) = Grade(ea, q);
                    ea.CorrectAnswerId = q.Answers.FirstOrDefault(a => a.IsCorrect)?.Id;
                    ea.UpdatedAt = now;
                }

                // ── Group theo Section ─────────────────────────────
                var sectionGroups = examAnswers
                    .GroupBy(a => new
                    {
                        SectionId = a.ExamQuestions.ExamSectionId,
                        SectionName = a.ExamQuestions.ExamSection?.Category?.Name ?? "Unknown",
                        CategoryCode = a.ExamQuestions.ExamSection?.Category?.Code ?? "",
                    })
                    .OrderBy(g => g.Key.CategoryCode)
                    .ToList();

                // ── Tính Listening/Reading correct ────────────────
                int listeningCorrect = 0, readingCorrect = 0;
                foreach (var grp in sectionGroups)
                {
                    var code = grp.Key.CategoryCode.ToUpperInvariant();
                    var count = grp.Count(a => a.IsCorrect);
                    if (code.Contains("_L_")) listeningCorrect += count;
                    else if (code.Contains("_R_")) readingCorrect += count;
                }

                // ── Band score từ DB hoặc fallback ────────────────
                var listeningBand = await GetBandScoreAsync("Listening", listeningCorrect, ct);
                var readingBand = await GetBandScoreAsync("Reading", readingCorrect, ct);

                // ── SectionResult ─────────────────────────────────
                var sectionResults = sectionGroups.Select(g => new ExamSectionResult
                {
                    Id = Guid.NewGuid(),
                    ExamAttemptId = attempt.Id,
                    ExamSectionId = g.Key.SectionId,
                    TotalQuestions = g.Count(),
                    CorrectAnswers = g.Count(a => a.IsCorrect),
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToList();

                var correctTotal = examAnswers.Count(a => a.IsCorrect);
                var skipped = examAnswers.Count(a => !a.IsAnswered);
                var wrong = examAnswers.Count(a => a.IsAnswered && !a.IsCorrect);

                attempt.Status = ExamAttemptStatus.Submitted;
                attempt.SubmitedAt = now;
                attempt.ActualTimeSeconds = (int)(now - attempt.StartedAt).TotalSeconds;
                attempt.ListeningCorrect = listeningCorrect;
                attempt.ListeningScore = (int)(listeningBand * 10);
                attempt.ReadingCorrect = readingCorrect;
                attempt.ReadingScore = (int)(readingBand * 10);
                attempt.TotalScore = (int)((listeningBand + readingBand) * 10);
                attempt.CorrectAnswers = correctTotal;
                attempt.IncorrectAnswers = wrong;
                attempt.UnanswerQuestions = skipped;
                attempt.UpdatedAt = now;

                _context.ExamSectionResults.AddRange(sectionResults);
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                await _mediator.Send(new UpdatePointsAndStreakCommand(
                     UserId: attempt.UserId,
                     PointsEarned: 5,   // Full test được +5 điểm
                     ActivityDate: DateTime.UtcNow
                 ), ct);

                return new IeltsSubmitExamResult
                {
                    AttemptId = attempt.Id,
                    SubmittedAt = now,
                    TotalQuestions = examAnswers.Count,
                    CorrectAnswers = correctTotal,
                    WrongAnswers = wrong,
                    SkippedAnswers = skipped,
                    Listening = new IeltsBandScore { CorrectCount = listeningCorrect, BandScore = listeningBand },
                    Reading = new IeltsBandScore { CorrectCount = readingCorrect, BandScore = readingBand },
                    Sections = sectionGroups.Select(g => new IeltsSectionResult
                    {
                        SectionName = g.Key.SectionName,
                        Total = g.Count(),
                        Correct = g.Count(a => a.IsCorrect),
                        Wrong = g.Count(a => a.IsAnswered && !a.IsCorrect),
                        Skipped = g.Count(a => !a.IsAnswered),
                    }).ToList(),
                };
            }
            catch { await tx.RollbackAsync(ct); throw; }
        }

        // ─────────────────────────────────────────────────────────
        // GRADE — chấm từng dạng câu hỏi
        // ─────────────────────────────────────────────────────────
        private static (bool isCorrect, double point) Grade(ExamAnswer ea, Question q)
        {
            var maxPoint = (double)ea.ExamQuestions.Point;

            switch (q.QuestionType)
            {
                // ── SingleChoice: Section 3 MCQ A/B/C, Reading MCQ ─
                case QuestionTypeEnum.SingleChoice:
                    {
                        if (!ea.SelectedAnswerId.HasValue) return (false, 0);
                        var correct = q.Answers.FirstOrDefault(a => a.IsCorrect);
                        var ok = correct != null && ea.SelectedAnswerId == correct.Id;
                        return (ok, ok ? maxPoint : 0);

                    }

                // ── MultipleChoice: Choose TWO (S2 Q11-12, S3 Q25-26)
                // TextAnswer = JSON ["id1","id2"] — frontend lưu 2 IDs
                // Chấm: phải đúng CẢ 2 → full point (IELTS không cho half mark)
                case QuestionTypeEnum.MultipleChoice:
                    {
                        if (!ea.IsAnswered || string.IsNullOrWhiteSpace(ea.TextAnswer)) return (false, 0);

                        var correctIds = q.Answers
                            .Where(a => a.IsCorrect)
                            .Select(a => a.Id.ToString())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        List<string> selectedIds;
                        try { selectedIds = JsonSerializer.Deserialize<List<string>>(ea.TextAnswer) ?? new(); }
                        catch { return (false, 0); }

                        var selectedSet = selectedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        var ok = correctIds.Count == selectedSet.Count && correctIds.SetEquals(selectedSet);
                        return (ok, ok ? maxPoint : 0);
                    }

                // ── Fill-in (S1 FormCompletion, S4 NoteCompletion) ──
                // Map Labeling (S2 Q15-20): user điền "H" → Normalize → compare
                // Chấp nhận "h"/"H" → DB có 2 Answer IsCorrect=true
                case QuestionTypeEnum.FormCompletion:
                case QuestionTypeEnum.NoteCompletion:
                case QuestionTypeEnum.SentenceCompletion:
                case QuestionTypeEnum.ShortAnswer:
                case QuestionTypeEnum.MapLabeling:
                case QuestionTypeEnum.FillBlank:
                case QuestionTypeEnum.TableCompletion:
                case QuestionTypeEnum.SummaryCompletion:
                    {
                        if (!ea.IsAnswered || string.IsNullOrWhiteSpace(ea.TextAnswer)) return (false, 0);
                        var userInput = Normalize(ea.TextAnswer);
                        // Lấy TẤT CẢ answer IsCorrect (map có thể có H và h)
                        var correctSet = q.Answers
                             .Where(a => a.IsCorrect)
                             .SelectMany(a => (a.Content ?? "")
                                 .Split('/', StringSplitOptions.TrimEntries)
                                 .Select(Normalize))
                             .ToHashSet();
                        var ok = correctSet.Contains(userInput);
                        return (ok, ok ? maxPoint : 0);
                    }

                // ── Matching Features (S3 Q27-30): 1 letter / câu ───
                // ── Matching Information, MatchingHeading, MatchingSentenceEnds
                case QuestionTypeEnum.Matching:
                case QuestionTypeEnum.MatchingInformation:
                case QuestionTypeEnum.MatchingHeading:
                case QuestionTypeEnum.MatchingSentenceEnds:
                    {
                        if (!ea.IsAnswered || !ea.SelectedAnswerId.HasValue) return (false, 0);
                        var correct = q.Answers.FirstOrDefault(a => a.IsCorrect);
                        var ok = correct != null && ea.SelectedAnswerId == correct.Id;
                        return (ok, ok ? maxPoint : 0);
                    }

                // ── True/False/Not Given, Yes/No/Not Given ───────────
                case QuestionTypeEnum.TrueFalseNotGiven:
                case QuestionTypeEnum.YesNoNotGiven:
                    {
                        if (!ea.IsAnswered || !ea.SelectedAnswerId.HasValue) return (false, 0);
                        var correct = q.Answers.FirstOrDefault(a => a.IsCorrect);
                        var ok = correct != null && ea.SelectedAnswerId == correct.Id;
                        return (ok, ok ? maxPoint : 0);
                    }

                // ── Writing/Speaking — skip auto-grade ───────────────
                default:
                    ea.GradingStatus = GradingStatus.NotRequired;
                    return (false, 0);
            }
        }

        // ─────────────────────────────────────────────────────────
        // BAND SCORE — DB ScoreTable, fallback Cambridge table
        // ─────────────────────────────────────────────────────────
        private async Task<double> GetBandScoreAsync(string skill, int correct, CancellationToken ct)
        {
            if (correct <= 0) return 0;
            try
            {
                var code = skill == "Listening" ? "IELTS_L" : "IELTS_R";
                var table = await _context.ScoreTables
                    .AsNoTracking()
                    .Where(st => st.IsActive && !st.IsDeleted && st.SkillCategory.Code == code)
                    .Include(st => st.Entries)
                    .FirstOrDefaultAsync(ct);

                if (table?.Entries?.Any() == true)
                {
                    var entry = table.Entries
                        .Where(e => e.CorrectAnswers <= correct)
                        .OrderByDescending(e => e.CorrectAnswers)
                        .FirstOrDefault();
                    if (entry != null) return (double)entry.Score;
                }
            }
            catch { /* fallback */ }

            return skill == "Listening" ? ListeningBand(correct) : ReadingBand(correct);
        }

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

        private static string Normalize(string s) =>
                 System.Text.RegularExpressions.Regex
                     .Replace(s.Trim().ToLowerInvariant()
                               .Replace(".", "").Replace(",", "")
                               .Replace("'", "'"),   // normalize apostrophe
                               @"\s+", " ");
    }
}