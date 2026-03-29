using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Leaderboards.Commands;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.ExamAttempts.Commands
{
    public class SubmitExamCommand : IRequest<SubmitExamResult>
    {
        [Required]
        public Guid AttemptId { get; set; }
        public Guid UserId { get; set; }
        public bool IsAutoSubmit { get; set; } = false;
    }

    public class SubmitExamCommandHandler : IRequestHandler<SubmitExamCommand, SubmitExamResult>
    {
        private readonly IAppDbContext _context;
        private readonly IMediator _mediator;

        public SubmitExamCommandHandler(IAppDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        public async Task<SubmitExamResult> Handle(
            SubmitExamCommand request,
            CancellationToken cancellationToken)
        {
            // ── 1. Load attempt ───────────────────────────────────────────────────────
            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken)
                ?? throw new KeyNotFoundException($"Phiên thi {request.AttemptId} không tìm thấy");

            // ── 2. Auth check ─────────────────────────────────────────────────────────
            if (!request.IsAutoSubmit && attempt.UserId != request.UserId)
                throw new UnauthorizedAccessException("Không thể nộp bài của người dùng khác");

            // ── 3. Validate trạng thái ────────────────────────────────────────────────
            if (attempt.Status == ExamAttemptStatus.Submitted)
                throw new InvalidOperationException("Bài thi đã được nộp rồi");
            if (attempt.Status == ExamAttemptStatus.Abandoned)
                throw new InvalidOperationException("Bài thi đã bị hủy");
            if (!request.IsAutoSubmit && attempt.ExpiresAt.HasValue && attempt.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Thời gian làm bài đã hết");

            // ── 4. Load ExamAnswers ───────────────────────────────────────────────────
            var examAnswers = await _context.ExamAnswers
                .Where(a => a.ExamAttemptId == request.AttemptId)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.ExamSection)
                        .ThenInclude(s => s.Category)
                .Include(a => a.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)
                .ToListAsync(cancellationToken);

            if (!examAnswers.Any())
                throw new InvalidOperationException("Không tìm thấy câu hỏi trong bài thi");

            // ── 5. Load ScoreTable ────────────────────────────────────────────────────
            var scoreTables = await _context.ScoreTables
                .Where(st => st.IsActive && !st.IsDeleted)
                .Include(st => st.Entries)
                .ToListAsync(cancellationToken);

            // SkillCategoryId → ScoreTable
            var scoreTableBySkillId = scoreTables
                .ToDictionary(st => st.SkillCategoryId);

            // ── 6. Load Skill Categories để lấy Code ─────────────────────────────────
            var skillIds = scoreTables.Select(st => st.SkillCategoryId).ToHashSet();
            var skillCategoryById = await _context.Categories
                .Where(c => skillIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

            // ── 7. Query SectionId → SkillId thẳng từ DB ─────────────────────────────
            //
            // ExamSection.CategoryId → Categories(Part).ParentId = SkillId
            // Không dùng navigation property (có thể null do EF lazy load)
            //
            var sectionIds = examAnswers
                .Select(a => a.ExamQuestions.ExamSectionId)
                .Distinct()
                .ToList();

            var sectionToSkillId = await _context.ExamSections
                .Where(es => sectionIds.Contains(es.Id))
                .Join(
                    _context.Categories,
                    es => es.CategoryId,
                    part => part.Id,
                    (es, part) => new { SectionId = es.Id, SkillId = part.ParentId }
                )
                .Where(x => x.SkillId != null)
                .ToDictionaryAsync(
                    x => x.SectionId,
                    x => x.SkillId!.Value,
                    cancellationToken
                );

            var now = DateTime.UtcNow;

            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {
                // ── 8. Chấm điểm từng câu ────────────────────────────────────────────
                foreach (var ea in examAnswers)
                {
                    var correctAnswer = ea.ExamQuestions?.Question?.Answers
                        .FirstOrDefault(a => a.IsCorrect);

                    ea.CorrectAnswerId = correctAnswer?.Id;
                    ea.IsCorrect = ea.IsAnswered
                                 && ea.SelectedAnswerId.HasValue
                                 && ea.SelectedAnswerId == correctAnswer?.Id;
                    ea.Point = ea.IsCorrect ? (double)ea.ExamQuestions.Point : 0;
                    ea.UpdatedAt = now;
                }

                // ── 9. Group theo Section (Part) ──────────────────────────────────────
                var sectionGroups = examAnswers
                    .GroupBy(a => new
                    {
                        SectionId = a.ExamQuestions.ExamSectionId,
                        SectionName = a.ExamQuestions.ExamSection?.Category?.Name ?? "Unknown",
                    })
                    .ToList();

                // ── 10. Group theo Skill → tính điểm quy đổi ─────────────────────────
                int listeningCorrect = 0, listeningScore = 0;
                int readingCorrect = 0, readingScore = 0;

                // SkillId → converted score
                var skillConvertedScore = new Dictionary<Guid, int>();

                // Dùng sectionToSkillId (query từ DB) — không dùng navigation property
                var skillGroups = examAnswers
                    .Where(a => sectionToSkillId.ContainsKey(a.ExamQuestions.ExamSectionId))
                    .GroupBy(a => sectionToSkillId[a.ExamQuestions.ExamSectionId])
                    .ToList();

                foreach (var skillGroup in skillGroups)
                {
                    var skillId = skillGroup.Key;
                    var correctCount = skillGroup.Count(a => a.IsCorrect);

                    if (!scoreTableBySkillId.TryGetValue(skillId, out var scoreTable))
                        continue;

                    var converted = scoreTable.Entries
                        .FirstOrDefault(e => e.CorrectAnswers == correctCount)
                        ?.Score ?? scoreTable.MinScore;

                    skillConvertedScore[skillId] = converted;

                    if (!skillCategoryById.TryGetValue(skillId, out var skillCategory))
                        continue;

                    if (skillCategory.Code == "TOEIC_LISTENING")
                    {
                        listeningCorrect = correctCount;
                        listeningScore = converted;
                    }
                    else if (skillCategory.Code == "TOEIC_READING")
                    {
                        readingCorrect = correctCount;
                        readingScore = converted;
                    }
                }

                // ── 11. Tạo SectionResult ─────────────────────────────────────────────
                var sectionResults = sectionGroups.Select(g =>
                {
                    var skillId = sectionToSkillId.TryGetValue(g.Key.SectionId, out var sid)
                        ? sid : (Guid?)null;

                    var convertedScore = skillId.HasValue
                        && skillConvertedScore.TryGetValue(skillId.Value, out var cs)
                        ? cs : (int?)null;

                    return new ExamSectionResult
                    {
                        Id = Guid.NewGuid(),
                        ExamAttemptId = attempt.Id,
                        ExamSectionId = g.Key.SectionId,
                        TotalQuestions = g.Count(),
                        CorrectAnswers = g.Count(a => a.IsCorrect),
                        ConvertedScore = convertedScore,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                }).ToList();

                // ── 12. PartSummaries ─────────────────────────────────────────────────
                var partSummaries = sectionGroups.Select(g => new PartSummary
                {
                    PartName = g.Key.SectionName,
                    Total = g.Count(),
                    Correct = g.Count(a => a.IsCorrect),
                    Score = Math.Round(g.Sum(a => a.Point), 2),
                }).ToList();

                // ── 13. Tổng hợp ──────────────────────────────────────────────────────
                var totalScore = listeningScore + readingScore;
                var correctTotal = examAnswers.Count(a => a.IsCorrect);
                var skippedCount = examAnswers.Count(a => !a.IsAnswered);
                var wrongCount = examAnswers.Count(a => a.IsAnswered && !a.IsCorrect);
                var maxScore = examAnswers.Sum(a => (double)a.ExamQuestions.Point);

                // ── 14. Cập nhật Attempt ──────────────────────────────────────────────
                attempt.Status = ExamAttemptStatus.Submitted;
                attempt.SubmitedAt = now;
                attempt.ActualTimeSeconds = (int)(now - attempt.StartedAt).TotalSeconds;
                attempt.ListeningCorrect = listeningCorrect;
                attempt.ListeningScore = listeningScore;
                attempt.ReadingCorrect = readingCorrect;
                attempt.ReadingScore = readingScore;
                attempt.TotalScore = totalScore;
                attempt.CorrectAnswers = correctTotal;
                attempt.IncorrectAnswers = wrongCount;
                attempt.UnanswerQuestions = skippedCount;
                attempt.UpdatedAt = now;

                _context.ExamSectionResults.AddRange(sectionResults);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // ── 15. Cập nhật điểm & streak ───────────────────────────────────────
                await _mediator.Send(new UpdatePointsAndStreakCommand(
                    UserId: attempt.UserId,
                    PointsEarned: 5,
                    ActivityDate: DateTime.UtcNow
                ), cancellationToken);

                return new SubmitExamResult
                {
                    AttemptId = attempt.Id,
                    SubmittedAt = now,
                    TotalScore = totalScore,
                    MaxScore = Math.Round(maxScore, 2),
                    ScorePercent = maxScore > 0 ? Math.Round(totalScore / maxScore * 100, 1) : 0,
                    TotalQuestions = examAnswers.Count,
                    CorrectAnswers = correctTotal,
                    WrongAnswers = wrongCount,
                    SkippedAnswers = skippedCount,
                    DurationSeconds = (int)(now - attempt.StartedAt).TotalSeconds,
                    ListeningCorrect = listeningCorrect,
                    ListeningScore = listeningScore,
                    ReadingCorrect = readingCorrect,
                    ReadingScore = readingScore,
                    PartSummaries = partSummaries,
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}