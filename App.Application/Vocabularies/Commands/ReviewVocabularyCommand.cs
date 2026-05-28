using App.Application.DTOs.Vocabulary;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.Vocabularies.Commands
{
    // ============================================
    // 2. REVIEW A WORD (SM-2 Algorithm)
    // POST /api/vocabulary/review
    // ============================================

    public class ReviewVocabularyCommand : IRequest<VocabReviewResultDto>
    {
        public Guid UserId { get; set; }
        public Guid WordId { get; set; }
        public bool Remembered { get; set; } // true = Nhớ, false = Quên
    }

    public class ReviewVocabularyCommandHandler
        : IRequestHandler<ReviewVocabularyCommand, VocabReviewResultDto>
    {
        private readonly IAppDbContext _context;

        public ReviewVocabularyCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<VocabReviewResultDto> Handle(
            ReviewVocabularyCommand request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            // Load hoặc tạo mới progress
            var progress = await _context.UserVocabularyProgresses
                .FirstOrDefaultAsync(p =>
                    p.UserId == request.UserId
                    && p.WordId == request.WordId
                    && !p.IsDeleted,
                    cancellationToken);

            var word = await _context.VocabularyWords
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == request.WordId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy từ vựng");

            if (progress == null)
            {
                progress = new UserVocabularyProgress
                {
                    UserId = request.UserId,
                    WordId = request.WordId,
                    Status = "learning",
                    TimesReviewed = 0,
                    TimesCorrect = 0,
                    RepetitionCount = 0,
                    EaseFactor = 2.5,
                    Interval = 1,
                    CreatedAt = now,
                };
                _context.UserVocabularyProgresses.Add(progress);
            }

            // ── SM-2 Algorithm ──────────────────────────────
            progress.TimesReviewed++;
            progress.LastReviewedAt = now;

            if (request.Remembered)
            {
                progress.TimesCorrect++;
                progress.RepetitionCount++;

                // Tính interval mới
                if (progress.RepetitionCount == 1)
                    progress.Interval = 1;
                else if (progress.RepetitionCount == 2)
                    progress.Interval = 6;
                else
                    progress.Interval = (int)Math.Round(progress.Interval * progress.EaseFactor);

                // Tăng EaseFactor (max 2.5 + cộng thêm theo số lần đúng)
                progress.EaseFactor = Math.Min(
                    progress.EaseFactor + 0.1,
                    3.0); // Cap ở 3.0

                // Cập nhật status
                progress.Status = progress.RepetitionCount >= 5 ? "mastered"
                                : progress.RepetitionCount >= 2 ? "known"
                                : "learning";
            }
            else
            {
                // Quên → reset về đầu
                progress.RepetitionCount = 0;
                progress.Interval = 1;
                progress.EaseFactor = Math.Max(progress.EaseFactor - 0.2, 1.3); // Min 1.3
                progress.Status = "learning";
            }

            progress.NextReviewAt = now.AddDays(progress.Interval);
            progress.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);

            return new VocabReviewResultDto
            {
                WordId = request.WordId,
                Word = word.Word,
                Remembered = request.Remembered,
                NextReviewAt = progress.NextReviewAt.Value,
                NewInterval = progress.Interval,
                NewStatus = progress.Status,
            };
        }
    }

}
