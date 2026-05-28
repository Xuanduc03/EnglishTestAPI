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
    // 4. GET WORD LIST (phân trang)
    // GET /api/vocabulary/words
    // ============================================

    public class GetVocabularyWordsQuery : IRequest<PaginatedVocabResult>
    {
        public Guid? UserId { get; set; }       // Nếu có → trả kèm progress
        public string? Keyword { get; set; }
        public string? Status { get; set; }     // learning, known, mastered, new
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    

    public class GetVocabularyWordsQueryHandler
        : IRequestHandler<GetVocabularyWordsQuery, PaginatedVocabResult>
    {
        private readonly IAppDbContext _context;

        public GetVocabularyWordsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginatedVocabResult> Handle(
            GetVocabularyWordsQuery request,
            CancellationToken cancellationToken)
        {
            var wordQuery = _context.VocabularyWords
                .AsNoTracking()
                .Where(w => !w.IsDeleted);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var kw = request.Keyword.Trim().ToLower();
                wordQuery = wordQuery.Where(w =>
                    w.Word.ToLower().Contains(kw) ||
                    w.Meaning.ToLower().Contains(kw));
            }

            var total = await wordQuery.CountAsync(cancellationToken);

            var words = await wordQuery
                .OrderBy(w => w.OrderIndex)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Load progress nếu có UserId
            Dictionary<Guid, UserVocabularyProgress> progressMap = new();
            if (request.UserId.HasValue)
            {
                var wordIds = words.Select(w => w.Id).ToList();
                var progresses = await _context.UserVocabularyProgresses
                    .AsNoTracking()
                    .Where(p => p.UserId == request.UserId.Value
                             && wordIds.Contains(p.WordId)
                             && !p.IsDeleted)
                    .ToListAsync(cancellationToken);

                progressMap = progresses.ToDictionary(p => p.WordId);
            }

            // Filter theo status nếu có
            var dtos = words
                .Select(w =>
                {
                    progressMap.TryGetValue(w.Id, out var p);
                    var status = p?.Status ?? "new";
                    return new VocabWordDto
                    {
                        Id = w.Id,
                        Word = w.Word,
                        Phonetic = w.Phonetic,
                        PartOfSpeech = w.PartOfSpeech,
                        Meaning = w.Meaning,
                        Example = w.Example,
                        AudioUrl = w.AudioUrl,
                        ImageUrl = w.ImageUrl,
                        Level = w.Level,
                        Status = status,
                        TimesReviewed = p?.TimesReviewed,
                        NextReviewAt = p?.NextReviewAt,
                    };
                })
                .Where(d => string.IsNullOrWhiteSpace(request.Status)
                         || d.Status == request.Status)
                .ToList();

            return new PaginatedVocabResult
            {
                Items = dtos,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
            };
        }
    }
}
