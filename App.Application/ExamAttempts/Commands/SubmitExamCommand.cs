using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.ExamAttempts.Commands
{
    /// <summary>
    /// Command: Nộp bài + chấm điểm
    /// POST /api/exam-attempts/{attemptId}/submit
    /// </summary>
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

        // FIX: bỏ ICurrentUserService, dùng UserId từ command
        public SubmitExamCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<SubmitExamResult> Handle(
            SubmitExamCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Load attempt
            var attempt = await _context.ExamAttempts
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken)
                ?? throw new KeyNotFoundException($"Phiên thi {request.AttemptId} không tìm thấy");

            // 2. Auth check
            if (!request.IsAutoSubmit && attempt.UserId != request.UserId)
                throw new UnauthorizedAccessException("Không thể nộp bài của người dùng khác");

            // 3. Validate
            if (attempt.Status == ExamAttemptStatus.Submitted)
                throw new InvalidOperationException("Bài thi đã được nộp rồi");

            if (attempt.Status == ExamAttemptStatus.Abandoned)
                throw new InvalidOperationException("Bài thi đã bị hủy");

            // FIX: dùng DateTime.UtcNow nhất quán, không dùng DateTime.Now
            if (!request.IsAutoSubmit && attempt.ExpiresAt.HasValue && attempt.ExpiresAt < DateTime.UtcNow)
                throw new InvalidOperationException("Thời gian làm bài đã hết");

            // 4. Load ExamAnswers + đáp án đúng
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

            // 5. Load ScoreTable (Listening + Reading) — dùng chung toàn hệ thống
            var scoreTables = await _context.ScoreTables
                .Where(st => st.IsActive && !st.IsDeleted)
                .Include(st => st.Entries)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            using var transaction = await _context.BeginTransactionAsync(cancellationToken);
            try
            {

                // 6. Chấm điểm từng câu
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

                // 7. Group theo Section (Part) để tính SectionResult
                var sectionGroups = examAnswers
                    .GroupBy(a => new
                    {
                        SectionId = a.ExamQuestions.ExamSectionId,
                        SectionName = a.ExamQuestions.ExamSection?.Category?.Name ?? "Unknown",
                    })
                    .ToList();

                // 8. Group theo Skill (Parent của Part) để tính điểm TOEIC
                var skillGroups = examAnswers
                    .GroupBy(a => a.ExamQuestions.ExamSection?.Category?.Parent)
                    .Where(g => g.Key != null)
                    .ToList();

                // 9. Tính điểm Listening và Reading từ ScoreTable
                int listeningCorrect = 0, listeningScore = 0;
                int readingCorrect = 0, readingScore = 0;

                var sectionConvertedScores = new Dictionary<Guid, int>();

                foreach (var skillGroup in skillGroups)
                {
                    var skill = skillGroup.Key;
                    var correctCount = skillGroup.Count(a => a.IsCorrect);

                    // Lookup ScoreTable theo SkillCategoryId
                    var scoreTable = scoreTables
                        .FirstOrDefault(st => st.SkillCategoryId == skill.Id);

                    var converted = scoreTable?.Entries
                        .FirstOrDefault(e => e.CorrectAnswers == correctCount)
                        ?.Score ?? 0;

                    if (skill.Code == "LISTENING")
                    {
                        listeningCorrect = correctCount;
                        listeningScore = converted;
                    }
                    else if (skill.Code == "READING")
                    {
                        readingCorrect = correctCount;
                        readingScore = converted;
                    }

                    // Lưu ConvertedScore cho từng Section thuộc Skill này
                    // (chia đều converted score theo tỉ lệ câu đúng từng Part)
                    foreach (var sectionGroup in sectionGroups)
                    {
                        // Kiểm tra section này có thuộc skill hiện tại không
                        var sectionSkillId = examAnswers
                            .FirstOrDefault(a => a.ExamQuestions.ExamSectionId == sectionGroup.Key.SectionId)
                            ?.ExamQuestions?.ExamSection?.Category?.ParentId;

                        if (sectionSkillId == skill.Id)
                        {
                            sectionConvertedScores[sectionGroup.Key.SectionId] = converted;
                        }
                    }
                }

                // 10. Tạo SectionResult cho từng Part
                var sectionResults = sectionGroups.Select(g => new ExamSectionResult
                {
                    Id = Guid.NewGuid(),
                    ExamAttemptId = attempt.Id,
                    ExamSectionId = g.Key.SectionId,
                    TotalQuestions = g.Count(),
                    CorrectAnswers = g.Count(a => a.IsCorrect),
                    // ConvertedScore là điểm của Skill chứa Section này
                    ConvertedScore = sectionConvertedScores.TryGetValue(g.Key.SectionId, out var cs) ? cs : null,
                    CreatedAt = now,
                    UpdatedAt = now,
                }).ToList();

                // 11. PartSummaries để trả về response
                var partSummaries = sectionGroups.Select(g => new PartSummary
                {
                    PartName = g.Key.SectionName,
                    Total = g.Count(),
                    Correct = g.Count(a => a.IsCorrect),
                    Score = Math.Round(g.Sum(a => a.Point), 2),
                }).ToList();

                // 12. Tính tổng
                var totalScore = listeningScore + readingScore;
                var correctTotal = examAnswers.Count(a => a.IsCorrect);
                var skippedCount = examAnswers.Count(a => !a.IsAnswered);
                var wrongCount = examAnswers.Count(a => a.IsAnswered && !a.IsCorrect);
                var maxScore = examAnswers.Sum(a => (double)a.ExamQuestions.Point);

                // 13. Update attempt với đầy đủ thông tin skill
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
                    // Thêm thông tin skill vào response
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