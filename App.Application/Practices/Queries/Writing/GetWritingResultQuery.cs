using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace App.Application.Writing.Queries
{
    // ============================================================
    // GET WRITING RESULT QUERY
    // Lấy kết quả sau khi AI chấm xong
    // Tính điểm TOEIC Writing (0-200) từ rubric scores
    // ============================================================

    public record GetWritingResultQuery(
        Guid SessionId,
        Guid UserId
    ) : IRequest<WritingSessionResultDto>;

    public class GetWritingResultQueryHandler
        : IRequestHandler<GetWritingResultQuery, WritingSessionResultDto>
    {
        private readonly IAppDbContext _context;

        public GetWritingResultQueryHandler(IAppDbContext context)
            => _context = context;

        public async Task<WritingSessionResultDto> Handle(
            GetWritingResultQuery request,
            CancellationToken cancellationToken)
        {
            var attempt = await _context.PracticeAttempts
                .AsNoTracking()
                .Include(a => a.Answers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId
                                       && a.UserId == request.UserId,
                                    cancellationToken)
                ?? throw new InvalidOperationException("Writing session not found.");

            var partResults = await _context.PracticePartResults
                .AsNoTracking()
                .Where(p => p.PracticeAttemptId == attempt.Id)
                .OrderBy(p => p.PartNumber)
                .ToListAsync(cancellationToken);

            // Nhóm answers theo CategoryId (= PartId)
            var answersByCategory = attempt.Answers
                .GroupBy(a => a.Question.CategoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var gradedCount = attempt.Answers.Count(a =>
                a.GradingStatus == GradingStatusEnum.Completed
             || a.GradingStatus == GradingStatusEnum.Failed);

            bool isFullyGraded = gradedCount == attempt.Answers.Count
                               && attempt.Status == AttemptStatus.Completed;

            // Build PartResultDtos
            var partResultDtos = partResults.Select(partResult =>
            {
                var answers = answersByCategory.TryGetValue(partResult.CategoryId, out var ans)
                    ? ans : new List<PracticeAnswer>();

                var questionResults = answers
                    .OrderBy(a => a.OrderIndex)
                    .Select((a, idx) => MapQuestionResult(a, idx + 1))
                    .ToList();

                double? avgScore = questionResults
                    .Where(q => q.GradingResult != null)
                    .Select(q => q.GradingResult!.Score)
                    .DefaultIfEmpty()
                    .Average();

                return new WritingPartResultDto
                {
                    PartName = partResult.PartName,
                    PartNumber = partResult.PartNumber,
                    TotalQuestions = partResult.TotalQuestions,
                    GradedQuestions = answers.Count(a => a.GradingStatus == GradingStatusEnum.Completed),
                    AverageScore = questionResults.Any(q => q.GradingResult != null)
                        ? Math.Round(avgScore ?? 0, 2)
                        : null,
                    QuestionResults = questionResults
                };
            }).ToList();

            // Tính tổng điểm TOEIC Writing (thang 0-200)
            double? toeicScore = isFullyGraded
                ? CalculateToeicWritingScore(partResultDtos)
                : null;

            // Cập nhật Score trong DB nếu fully graded
            if (isFullyGraded && toeicScore.HasValue)
                await PersistFinalScoreAsync(attempt.Id, toeicScore.Value, gradedCount, cancellationToken);

            return new WritingSessionResultDto
            {
                SessionId = attempt.Id,
                Title = attempt.Title,
                TotalQuestions = attempt.TotalQuestions,
                GradedQuestions = gradedCount,
                IsFullyGraded = isFullyGraded,
                OverallScore = toeicScore,
                TotalTimeSeconds = attempt.TotalTimeSeconds ?? 0,
                PartResults = partResultDtos
            };
        }

        // ── Mapping ──────────────────────────────────────────────────

        private static WritingQuestionResultDto MapQuestionResult(PracticeAnswer a, int questionNumber)
        {
            WritingGradingResultDto? gradingResult = null;

            if (a.GradingStatus == GradingStatusEnum.Completed && !string.IsNullOrWhiteSpace(a.AiFeedback))
            {
                gradingResult = new WritingGradingResultDto
                {
                    Feedback = a.AiFeedback,
                    Breakdown = ParseScoreBreakdown(a.AiScoreDetailJson)
                };

                // Score tổng từ Breakdown (trung bình các tiêu chí)
                gradingResult.Score = ComputeScoreFromBreakdown(gradingResult.Breakdown);
            }

            return new WritingQuestionResultDto
            {
                QuestionId = a.QuestionId,
                QuestionNumber = questionNumber,
                TextAnswer = a.TextAnswer ?? string.Empty,
                GradingResult = gradingResult,
                GradingStatus = MapGradingStatus(a.GradingStatus)
            };
        }

        private static WritingScoreBreakdown? ParseScoreBreakdown(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<WritingScoreBreakdown>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        private static double ComputeScoreFromBreakdown(WritingScoreBreakdown? b)
        {
            if (b == null) return 0;

            var scores = new[]
            {
                b.GrammarScore, b.RelevanceScore,
                b.ContentScore, b.OrganizationScore, b.VocabularyScore,
                b.ArgumentScore, b.CoherenceScore
            }
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .ToList();

            return scores.Count > 0 ? Math.Round(scores.Average(), 2) : 0;
        }

        // ── TOEIC Writing Score Calculation ──────────────────────────
        // Quy đổi raw scores thành thang điểm TOEIC Writing (0-200)
        // Part 1: 8 câu × tối đa 5 điểm = 40 pts raw → 0-100 scaled
        // Part 2: 2 câu × tối đa 4 điểm = 8 pts raw  → 0-60 scaled
        // Part 3: 1 câu × tối đa 5 điểm = 5 pts raw  → 0-40 scaled
        // Tổng: 0-200

        private static double CalculateToeicWritingScore(List<WritingPartResultDto> partResults)
        {
            double part1Score = 0, part2Score = 0, part3Score = 0;

            foreach (var part in partResults)
            {
                var rawTotal = part.QuestionResults
                    .Where(q => q.GradingResult != null)
                    .Sum(q => q.GradingResult!.Score);

                switch (part.PartNumber)
                {
                    case 1:
                        // Max raw = 8 × 5 = 40 → scale to 100
                        part1Score = Math.Min(rawTotal / 40.0 * 100, 100);
                        break;
                    case 2:
                        // Max raw = 2 × 4 = 8 → scale to 60
                        part2Score = Math.Min(rawTotal / 8.0 * 60, 60);
                        break;
                    case 3:
                        // Max raw = 1 × 5 = 5 → scale to 40
                        part3Score = Math.Min(rawTotal / 5.0 * 40, 40);
                        break;
                }
            }

            return Math.Round(part1Score + part2Score + part3Score, 0);
        }

        private static WritingGradingStatus MapGradingStatus(GradingStatusEnum s) => s switch
        {
            GradingStatusEnum.NotRequired => WritingGradingStatus.Pending,
            GradingStatusEnum.Pending => WritingGradingStatus.Pending,
            GradingStatusEnum.Grading => WritingGradingStatus.Grading,
            GradingStatusEnum.Completed => WritingGradingStatus.Completed,
            GradingStatusEnum.Failed => WritingGradingStatus.Failed,
            _ => WritingGradingStatus.Pending
        };

        private async Task PersistFinalScoreAsync(
            Guid attemptId,
            double toeicScore,
            int gradedCount,
            CancellationToken cancellationToken)
        {
            var attempt = await _context.PracticeAttempts
                .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

            if (attempt == null || attempt.Score == toeicScore) return; // Đã lưu rồi

            attempt.Score = toeicScore;
            attempt.CorrectAnswers = gradedCount;
            attempt.AccuracyPercentage = attempt.TotalQuestions > 0
                ? Math.Round((double)gradedCount / attempt.TotalQuestions * 100, 2)
                : 0;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // ============================================================
    // GET WRITING REVIEW QUERY
    // Xem lại bài làm + feedback chi tiết từng câu
    // ============================================================

    public record GetWritingReviewQuery(
        Guid SessionId,
        Guid UserId
    ) : IRequest<WritingReviewDto>;

    public class GetWritingReviewQueryHandler
        : IRequestHandler<GetWritingReviewQuery, WritingReviewDto>
    {
        private readonly IAppDbContext _context;

        public GetWritingReviewQueryHandler(IAppDbContext context)
            => _context = context;

        public async Task<WritingReviewDto> Handle(
            GetWritingReviewQuery request,
            CancellationToken cancellationToken)
        {
            var attempt = await _context.PracticeAttempts
                .AsNoTracking()
                .Include(a => a.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Media)
                .FirstOrDefaultAsync(a => a.Id == request.SessionId
                                       && a.UserId == request.UserId,
                                    cancellationToken)
                ?? throw new InvalidOperationException("Writing session not found.");

            var questions = attempt.Answers
                .OrderBy(a => a.OrderIndex)
                .Select((a, idx) => new WritingQuestionResultDto
                {
                    QuestionId = a.QuestionId,
                    QuestionNumber = idx + 1,
                    TextAnswer = a.TextAnswer ?? string.Empty,
                    GradingStatus = a.GradingStatus switch
                    {
                        GradingStatusEnum.Completed => WritingGradingStatus.Completed,
                        GradingStatusEnum.Grading => WritingGradingStatus.Grading,
                        GradingStatusEnum.Failed => WritingGradingStatus.Failed,
                        _ => WritingGradingStatus.Pending
                    },
                    GradingResult = a.GradingStatus == GradingStatusEnum.Completed
                        ? new WritingGradingResultDto
                        {
                            Feedback = a.AiFeedback ?? string.Empty,
                            Breakdown = string.IsNullOrWhiteSpace(a.AiScoreDetailJson)
                                ? null
                                : TryDeserialize(a.AiScoreDetailJson)
                        }
                        : null
                })
                .ToList();

            return new WritingReviewDto
            {
                SessionId = attempt.Id,
                Title = attempt.Title,
                Questions = questions
            };
        }

        private static WritingScoreBreakdown? TryDeserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<WritingScoreBreakdown>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }
    }
}