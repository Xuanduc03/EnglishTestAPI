using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs.Vocabulary
{
    // ============================================
    // DTOs
    // ============================================

    public class FlashcardDto
    {
        public Guid WordId { get; set; }
        public string Word { get; set; }
        public string Phonetic { get; set; }
        public string PartOfSpeech { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }

        // Mặt sau
        public string Meaning { get; set; }
        public string? Example { get; set; }
        public string? ExampleMeaning { get; set; }

        // Progress info
        public int TimesReviewed { get; set; }
        public int RepetitionCount { get; set; }
        public string Status { get; set; }
    }

    public class VocabTodayResultDto
    {
        public bool IsEmpty { get; set; }
        public string Message { get; set; } = "";
        public int TotalDue { get; set; }
        public List<FlashcardDto> Cards { get; set; } = new();
    }

    public class VocabReviewResultDto
    {
        public Guid WordId { get; set; }
        public string Word { get; set; }
        public bool Remembered { get; set; }
        public DateTime NextReviewAt { get; set; }
        public int NewInterval { get; set; }   // Số ngày đến lần ôn tiếp
        public string NewStatus { get; set; }
    }

    public class VocabSessionSummaryDto
    {
        public int TotalReviewed { get; set; }
        public int TotalRemembered { get; set; }
        public int TotalForgotten { get; set; }
        public double AccuracyPercent { get; set; }
        public int NewWords { get; set; }       // Từ mới học lần đầu
        public int MasteredWords { get; set; }  // Từ đã thành thạo hôm nay
        public List<VocabSummaryItemDto> Items { get; set; } = new();
    }

    public class VocabSummaryItemDto
    {
        public string Word { get; set; }
        public string Meaning { get; set; }
        public bool Remembered { get; set; }
        public int NextIntervalDays { get; set; }
    }

    // Phân trang 
    public class PaginatedVocabResult
    {
        public List<VocabWordDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class VocabWordDto
    {
        public Guid Id { get; set; }
        public string Word { get; set; }
        public string Phonetic { get; set; }
        public string PartOfSpeech { get; set; }
        public string Meaning { get; set; }
        public string? Example { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Level { get; set; }

        // Progress (null nếu chưa học)
        public string? Status { get; set; }
        public int? TimesReviewed { get; set; }
        public DateTime? NextReviewAt { get; set; }
    }
}
