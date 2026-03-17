using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.ExamAttempts.Queries
{
    /// <summary>
    /// QUERY: GET EXAM RESULT
    /// GET /api/exam-attempts/{attemptId}/result
    /// </summary>
    public class GetExamResultQuery : IRequest<ExamResultDto>
    {
        public Guid AttemptId { get; set; }
    }

    public class GetExamResultQueryHandler
        : IRequestHandler<GetExamResultQuery, ExamResultDto>
    {
        private readonly IAppDbContext _context;
        private readonly ICurrentUserService _userService;

        public GetExamResultQueryHandler(
            IAppDbContext context,
            ICurrentUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<ExamResultDto> Handle(
            GetExamResultQuery request,
            CancellationToken cancellationToken)
        {
            // 1. Load attempt + exam
            var attempt = await _context.ExamAttempts
                .AsNoTracking()
                .Include(e => e.Exam)
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken)
                ?? throw new KeyNotFoundException($"Attempt {request.AttemptId} not found");

            // 2. Auth check
            var currentUserId = _userService.UserId;
            if (currentUserId == null || attempt.UserId != currentUserId)
                throw new UnauthorizedAccessException("Access denied");

            // 3. Phải nộp bài mới xem được
            if (attempt.Status != Domain.Entities.ExamAttemptStatus.Submitted)
                throw new InvalidOperationException("Exam has not been submitted yet");

            // 4. Load section results — include đến Skill (Parent của Part)
            var sectionResults = await _context.ExamSectionResults
                .AsNoTracking()
                .Include(e => e.Section)
                    .ThenInclude(s => s.Category)        // Part
                        .ThenInclude(c => c.Parent)      // Skill (LISTENING / READING)
                .Where(e => e.ExamAttemptId == request.AttemptId)
                .ToListAsync(cancellationToken);

            // 5. Load answer stats
            var answerStats = await _context.ExamAnswers
                .AsNoTracking()
                .Where(e => e.ExamAttemptId == request.AttemptId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Correct = g.Count(a => a.IsCorrect),
                    Skipped = g.Count(a => !a.IsAnswered),
                    RawScore = g.Sum(a => a.Point),
                    MaxScore = g.Sum(a => (double)a.ExamQuestions.Point),
                })
                .FirstOrDefaultAsync(cancellationToken);

            // 6. Lấy điểm Listening / Reading trực tiếp từ attempt
            // (đã được set trong SubmitExamCommand)
            var listeningScore = attempt.ListeningScore;
            var listeningCorrect = attempt.ListeningCorrect;
            var readingScore = attempt.ReadingScore;
            var readingCorrect = attempt.ReadingCorrect;
            var totalToeicScore = attempt.TotalScore;

            var isToeic = listeningScore.HasValue && readingScore.HasValue;

            // 7. Build SectionResultDto — gom theo Skill
            var sectionDtos = sectionResults.Select(sr => new SectionResultDto
            {
                SectionId = sr.ExamSectionId,
                SectionName = sr.Section?.Category?.Name ?? "Unknown",
                SkillCode = sr.Section?.Category?.Parent?.Code ?? "",
                SkillName = sr.Section?.Category?.Parent?.Name ?? "",
                TotalQuestions = sr.TotalQuestions,
                CorrectAnswers = sr.CorrectAnswers,
                WrongAnswers = sr.TotalQuestions - sr.CorrectAnswers,
                // ConvertedScore là điểm Skill (Listening/Reading) đã lưu khi submit
                ToeicConvertedScore = sr.ConvertedScore,
                Score = sr.TotalQuestions > 0
                    ? Math.Round((double)sr.CorrectAnswers / sr.TotalQuestions * 100, 1)
                    : 0,
            }).ToList();

            // 8. Build response
            var submittedAt = attempt.SubmitedAt ?? DateTime.UtcNow;

            return new ExamResultDto
            {
                AttemptId = attempt.Id,
                ExamTitle = attempt.Exam?.Title ?? string.Empty,
                ExamCode = attempt.Exam?.Code ?? string.Empty,
                StartedAt = attempt.StartedAt,
                SubmittedAt = submittedAt,
                DurationSeconds = (int)(submittedAt - attempt.StartedAt).TotalSeconds,

                // Thống kê câu trả lời
                TotalQuestions = answerStats?.Total ?? attempt.TotalQuestions,
                CorrectAnswers = answerStats?.Correct ?? attempt.CorrectAnswers,
                WrongAnswers = answerStats != null
                    ? answerStats.Total - answerStats.Correct - answerStats.Skipped
                    : attempt.IncorrectAnswers,
                SkippedAnswers = answerStats?.Skipped ?? attempt.UnanswerQuestions,
                RawScore = Math.Round(answerStats?.RawScore ?? 0, 2),
                MaxScore = Math.Round(answerStats?.MaxScore ?? 0, 2),
                ScorePercent = answerStats?.MaxScore > 0
                    ? Math.Round(answerStats.RawScore / answerStats.MaxScore * 100, 1)
                    : 0,

                // Điểm TOEIC
                IsToeic = isToeic,
                ListeningCorrect = listeningCorrect,
                ListeningScore = listeningScore,
                ReadingCorrect = readingCorrect,
                ReadingScore = readingScore,
                TotalToeicScore = totalToeicScore,

                SectionResults = sectionDtos,
            };
        }
    }
}