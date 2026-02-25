using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.ExamAttempts.Queries
{
    /// <summary>
    /// QUERY: GET EXAM RESULT
    // GET /api/exam-attempts/{attemptId}/result
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
            // load attempt + exam info
            var attempt = await _context.ExamAttempts
                .AsNoTracking()
                .Include(e => e.Exam)
                .FirstOrDefaultAsync(a => a.Id == request.AttemptId, cancellationToken) ?? throw new KeyNotFoundException($"Attempt {request.AttemptId} not found");

            // 2, auth check
            var currentUser = _userService.UserId;
            if (currentUser == null && attempt.UserId != currentUser)
            {
                throw new UnauthorizedAccessException("Access denied");
            }

            // 3. phải nộp bài mới xem dc 
            if(attempt.Status != Domain.Entities.ExamAttemptStatus.Submitted)
            {
                throw new InvalidOperationException("Exam has not been submitted yet");
            }

            // 4. load section result 
            var sectionResults = await _context.ExamSectionResults
                .AsNoTracking()
                .Include(e => e.Section)
                    .ThenInclude(s => s.Category)
                .Where(e => e.ExamAttemptId == request.AttemptId)
                .ToListAsync(cancellationToken);

            //5. load exam answer để tính điểm thô 
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
            //// 6. Xác định có phải TOEIC không (dựa vào Category name của sections)
            //var skillTypes = sectionResults
            //    .Select(sr => sr.SkillType?.ToLower())
            //    .ToHashSet();

            //var isToeic = skillTypes.Contains("listening") && skillTypes.Contains("reading");

            //// 7. Lấy điểm TOEIC từng skill
            //int? listeningScore = null, readingScore = null;
            //if (isToeic)
            //{
            //    listeningScore = sectionResults
            //        .Where(sr => sr.SkillType?.ToLower() == "listening")
            //        .Sum(sr => sr.ConvertedScore);

            //    readingScore = sectionResults
            //        .Where(sr => sr.SkillType?.ToLower() == "reading")
            //        .Sum(sr => sr.ConvertedScore);
            //}

            // 8. Build DTO
            var submittedAt = attempt.SubmitedAt ?? DateTime.UtcNow;

            return new ExamResultDto
            {
                AttemptId = attempt.Id,
                ExamTitle = attempt.Exam?.Title ?? string.Empty,
                ExamCode = attempt.Exam?.Code ?? string.Empty,
                StartedAt = attempt.StartedAt,
                SubmittedAt = submittedAt,
                DurationSeconds = (int)(submittedAt - attempt.StartedAt).TotalSeconds,

                TotalQuestions = answerStats?.Total ?? 0,
                CorrectAnswers = answerStats?.Correct ?? 0,
                WrongAnswers = (answerStats?.Total ?? 0)
                                - (answerStats?.Correct ?? 0)
                                - (answerStats?.Skipped ?? 0),
                SkippedAnswers = answerStats?.Skipped ?? 0,
                RawScore = Math.Round(answerStats?.RawScore ?? 0, 2),
                MaxScore = Math.Round(answerStats?.MaxScore ?? 0, 2),
                ScorePercent = answerStats?.MaxScore > 0
                    ? Math.Round((answerStats.RawScore / answerStats.MaxScore) * 100, 1)
                    : 0,

                //IsToeic = isToeic,
                //ListeningScore = listeningScore,
                //ReadingScore = readingScore,
                //TotalToeicScore = isToeic ? (listeningScore ?? 0) + (readingScore ?? 0) : null,

                SectionResults = sectionResults.Select(sr => new SectionResultDto
                {
                    //SectionName = sr.Section?.Category?.Name ?? sr.SkillType,
                    //SkillType = sr.SkillType,
                    TotalQuestions = sr.TotalQuestions,
                    CorrectAnswers = sr.CorrectAnswers,
                    Score = 0, // nếu cần tính thêm từ ExamAnswer
                    ToeicConvertedScore = sr.ConvertedScore,
                }).ToList(),
            };
        }
    }
}
