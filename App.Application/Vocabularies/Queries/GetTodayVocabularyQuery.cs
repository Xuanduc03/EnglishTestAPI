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

namespace App.Application.Vocabularies.Queries
{
    // ============================================
    // 1. GET TODAY'S FLASHCARDS
    // GET /api/vocabulary/today
    // ============================================

    public class GetTodayVocabularyQuery : IRequest<VocabTodayResultDto>
    {
        public Guid UserId { get; set; }
        public int MaxCards { get; set; } = 20; // Tối đa 20 thẻ/ngày
    }
    public class GetTodayVocabularyQueryHandler
      : IRequestHandler<GetTodayVocabularyQuery, VocabTodayResultDto>
    {
        private readonly IAppDbContext _context;

        public GetTodayVocabularyQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<VocabTodayResultDto> Handle(
            GetTodayVocabularyQuery request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            // 1. Từ đã có progress + đến hạn ôn hôm nay
            var dueWords = await _context.UserVocabularyProgresses
                .AsNoTracking()
                .Where(p => p.UserId == request.UserId
                         && !p.IsDeleted
                         && p.NextReviewAt <= now
                         && p.Status != "mastered")
                .Include(p => p.Word)
                .OrderBy(p => p.NextReviewAt) // Ưu tiên từ quá hạn lâu nhất
                .Take(request.MaxCards)
                .ToListAsync(cancellationToken);

            // 2. Nếu chưa đủ MaxCards → lấy thêm từ mới chưa học
            var remaining = request.MaxCards - dueWords.Count;
            List<VocabularyWord> newWords = new();

            if (remaining > 0)
            {
                var learnedWordIds = await _context.UserVocabularyProgresses
                    .Where(p => p.UserId == request.UserId && !p.IsDeleted)
                    .Select(p => p.WordId)
                    .ToListAsync(cancellationToken);

                newWords = await _context.VocabularyWords
                    .AsNoTracking()
                    .Where(w => !w.IsDeleted && !learnedWordIds.Contains(w.Id))
                    .OrderBy(w => w.OrderIndex)
                    .Take(remaining)
                    .ToListAsync(cancellationToken);
            }

            var totalDue = dueWords.Count + newWords.Count;

            if (totalDue == 0)
            {
                return new VocabTodayResultDto
                {
                    IsEmpty = true,
                    Message = "Bạn đã hoàn thành bài học hôm nay",
                    TotalDue = 0,
                    Cards = new()
                };
            }

            // 3. Build flashcards
            var cards = new List<FlashcardDto>();

            // Từ đến hạn ôn
            cards.AddRange(dueWords.Select(p => new FlashcardDto
            {
                WordId = p.WordId,
                Word = p.Word.Word,
                Phonetic = p.Word.Phonetic,
                PartOfSpeech = p.Word.PartOfSpeech,
                AudioUrl = p.Word.AudioUrl,
                ImageUrl = p.Word.ImageUrl,
                Meaning = p.Word.Meaning,
                Example = p.Word.Example,
                ExampleMeaning = p.Word.ExampleMeaning,
                TimesReviewed = p.TimesReviewed,
                RepetitionCount = p.RepetitionCount,
                Status = p.Status,
            }));

            // Từ mới chưa học
            cards.AddRange(newWords.Select(w => new FlashcardDto
            {
                WordId = w.Id,
                Word = w.Word,
                Phonetic = w.Phonetic,
                PartOfSpeech = w.PartOfSpeech,
                AudioUrl = w.AudioUrl,
                ImageUrl = w.ImageUrl,
                Meaning = w.Meaning,
                Example = w.Example,
                ExampleMeaning = w.ExampleMeaning,
                TimesReviewed = 0,
                RepetitionCount = 0,
                Status = "new",
            }));

            return new VocabTodayResultDto
            {
                IsEmpty = false,
                TotalDue = totalDue,
                Cards = cards,
            };
        }
    }
}
