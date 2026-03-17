using App.Application.DTOs;
using App.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Vocabularies.Queries
{
    // ============================================
    // 3. GET SESSION SUMMARY
    // GET /api/vocabulary/summary?date=2026-03-15
    // ============================================

    public class GetVocabSessionSummaryQuery : IRequest<VocabSessionSummaryDto>
    {
        public Guid UserId { get; set; }
        public DateTime? Date { get; set; } // Mặc định hôm nay
    }

    public class GetVocabSessionSummaryQueryHandler
        : IRequestHandler<GetVocabSessionSummaryQuery, VocabSessionSummaryDto>
    {
        private readonly IAppDbContext _context;

        public GetVocabSessionSummaryQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<VocabSessionSummaryDto> Handle(
            GetVocabSessionSummaryQuery request,
            CancellationToken cancellationToken)
        {
            var targetDate = (request.Date ?? DateTime.UtcNow).Date;
            var from = targetDate;
            var to = targetDate.AddDays(1);

            // Lấy tất cả từ đã review hôm nay
            var reviewed = await _context.UserVocabularyProgresses
                .AsNoTracking()
                .Where(p => p.UserId == request.UserId
                         && !p.IsDeleted
                         && p.LastReviewedAt >= from
                         && p.LastReviewedAt < to)
                .Include(p => p.Word)
                .ToListAsync(cancellationToken);

            if (!reviewed.Any())
            {
                return new VocabSessionSummaryDto
                {
                    TotalReviewed = 0,
                    TotalRemembered = 0,
                    TotalForgotten = 0,
                    AccuracyPercent = 0,
                    Items = new()
                };
            }

            // Remembered = RepetitionCount > 0 sau khi review hôm nay
            var remembered = reviewed.Count(p => p.RepetitionCount > 0);
            var forgotten = reviewed.Count - remembered;
            var mastered = reviewed.Count(p => p.Status == "mastered");
            var newWords = reviewed.Count(p => p.TimesReviewed == 1);

            return new VocabSessionSummaryDto
            {
                TotalReviewed = reviewed.Count,
                TotalRemembered = remembered,
                TotalForgotten = forgotten,
                AccuracyPercent = reviewed.Count > 0
                    ? Math.Round((double)remembered / reviewed.Count * 100, 1)
                    : 0,
                NewWords = newWords,
                MasteredWords = mastered,
                Items = reviewed.Select(p => new VocabSummaryItemDto
                {
                    Word = p.Word.Word,
                    Meaning = p.Word.Meaning,
                    Remembered = p.RepetitionCount > 0,
                    NextIntervalDays = p.Interval,
                }).ToList()
            };
        }
    }
}
