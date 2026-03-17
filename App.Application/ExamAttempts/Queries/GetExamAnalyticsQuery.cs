using App.Application.Interfaces;
using App.Application.Services.Interface;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace App.Application.ExamAttempts.Queries
{
    // ============================================================
    // QUERY: GET EXAM ANALYTICS
    // GET /api/exam-attempts/analytics
    // Phân tích điểm yếu/mạnh + lộ trình cải thiện
    // ============================================================
    public class GetExamAnalyticsQuery : IRequest<ExamAnalyticsDto>
    {
        public Guid UserId { get; set; }
        public int LastN { get; set; } = 5; // Phân tích N lần thi gần nhất
    }

    // ── DTOs ────────────────────────────────────────────────────
    public class ExamAnalyticsDto
    {
        // Tổng quan
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
        public double BestScore { get; set; }
        public double LatestScore { get; set; }
        public double ScoreTrend { get; set; } // + tăng / - giảm so với lần trước

        // Phân tích theo Part
        public List<PartAnalyticsDto> PartAnalytics { get; set; } = new();

        // Điểm yếu / mạnh
        public List<string> Strengths { get; set; } = new();    // Part làm tốt ≥ 70%
        public List<string> Weaknesses { get; set; } = new();   // Part làm kém < 50%

        // Lộ trình cải thiện
        public List<ImprovementSuggestionDto> Suggestions { get; set; } = new();

        // Lịch sử điểm (cho biểu đồ)
        public List<ScoreHistoryDto> ScoreHistory { get; set; } = new();
    }

    public class PartAnalyticsDto
    {
        public string PartName { get; set; }          // "Part 1", "Part 2"...
        public string Skill { get; set; }             // "Listening" / "Reading"
        public double AccuracyPercent { get; set; }   // % đúng trung bình
        public int TotalAttempted { get; set; }       // Tổng số câu đã làm
        public int TotalCorrect { get; set; }
        public double TrendPercent { get; set; }      // So với lần trước
        public string Level { get; set; }             // "Strong" / "Average" / "Weak"
    }

    public class ImprovementSuggestionDto
    {
        public string PartName { get; set; }
        public string Priority { get; set; }          // "High" / "Medium" / "Low"
        public string Message { get; set; }           // "Cần ôn luyện thêm..."
        public string ActionUrl { get; set; }         // Link đến practice
    }

    public class ScoreHistoryDto
    {
        public DateTime AttemptDate { get; set; }
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public double Percent { get; set; }
        public string ExamTitle { get; set; }
    }

    // ── Handler ─────────────────────────────────────────────────
    public class GetExamAnalyticsQueryHandler
        : IRequestHandler<GetExamAnalyticsQuery, ExamAnalyticsDto>
    {
        private readonly IAppDbContext _context;

        public GetExamAnalyticsQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<ExamAnalyticsDto> Handle(
            GetExamAnalyticsQuery request,
            CancellationToken ct)
        {
            // 1. Load N lần thi gần nhất đã nộp
            var attempts = await _context.ExamAttempts
                .AsNoTracking()
                .Where(a => a.UserId == request.UserId
                         && a.Status == Domain.Entities.ExamAttemptStatus.Submitted
                         && !a.IsDeleted)
                .Include(a => a.Exam)
                .OrderByDescending(a => a.SubmitedAt)
                .Take(request.LastN)
                .ToListAsync(ct);

            if (!attempts.Any())
                return new ExamAnalyticsDto();

            // 2. Load section results của các attempts đó
            var attemptIds = attempts.Select(a => a.Id).ToList();

            var sectionResults = await _context.ExamSectionResults
                .AsNoTracking()
                .Where(sr => attemptIds.Contains(sr.ExamAttemptId) && !sr.IsDeleted)
                .Include(sr => sr.Section)
                    .ThenInclude(s => s.Category)
                .ToListAsync(ct);

            // 3. Tính score history
            var scoreHistory = attempts
                .OrderBy(a => a.SubmitedAt)
                .Select(a => new ScoreHistoryDto
                {
                    AttemptDate = a.SubmitedAt ?? a.StartedAt,
                    Score = a.TotalScore ?? 0,
                    MaxScore = sectionResults
                        .Where(sr => sr.ExamAttemptId == a.Id)
                        .Sum(sr => (double?)sr.TotalQuestions) ?? a.TotalQuestions,
                    Percent = a.TotalQuestions > 0
                        ? Math.Round((double)(a.TotalScore ?? 0) / a.TotalQuestions * 100, 1)
                        : 0,
                    ExamTitle = a.Exam?.Title ?? "Unknown",
                })
                .ToList();

            // 4. Phân tích theo Part — group tất cả section results theo tên Part
            var partGroups = sectionResults
                .GroupBy(sr => sr.Section?.Category?.Name ?? "Unknown")
                .Select(g =>
                {
                    var totalQ = g.Sum(sr => sr.TotalQuestions);
                    var totalC = g.Sum(sr => sr.CorrectAnswers);
                    var accuracy = totalQ > 0
                        ? Math.Round((double)totalC / totalQ * 100, 1) : 0;

                    // Trend: so sánh lần cuối vs lần trước
                    var ordered = g.OrderByDescending(sr =>
                        attempts.FirstOrDefault(a => a.Id == sr.ExamAttemptId)?.SubmitedAt)
                        .ToList();
                    double trend = 0;
                    if (ordered.Count >= 2)
                    {
                        var latest = ordered[0].TotalQuestions > 0
                            ? (double)ordered[0].CorrectAnswers / ordered[0].TotalQuestions * 100 : 0;
                        var previous = ordered[1].TotalQuestions > 0
                            ? (double)ordered[1].CorrectAnswers / ordered[1].TotalQuestions * 100 : 0;
                        trend = Math.Round(latest - previous, 1);
                    }

                    var partName = g.Key;
                    var skill = GetSkill(partName);
                    var level = accuracy >= 70 ? "Strong"
                                 : accuracy >= 50 ? "Average"
                                 : "Weak";

                    return new PartAnalyticsDto
                    {
                        PartName = partName,
                        Skill = skill,
                        AccuracyPercent = accuracy,
                        TotalAttempted = totalQ,
                        TotalCorrect = totalC,
                        TrendPercent = trend,
                        Level = level,
                    };
                })
                .OrderBy(p => p.PartName)
                .ToList();

            // 5. Strengths & Weaknesses
            var strengths = partGroups
                .Where(p => p.AccuracyPercent >= 70)
                .Select(p => p.PartName).ToList();

            var weaknesses = partGroups
                .Where(p => p.AccuracyPercent < 50)
                .Select(p => p.PartName).ToList();

            // 6. Suggestions — ưu tiên part yếu nhất
            var suggestions = partGroups
                .Where(p => p.AccuracyPercent < 70)
                .OrderBy(p => p.AccuracyPercent)
                .Take(3)
                .Select(p => new ImprovementSuggestionDto
                {
                    PartName = p.PartName,
                    Priority = p.AccuracyPercent < 40 ? "High"
                              : p.AccuracyPercent < 60 ? "Medium" : "Low",
                    Message = BuildSuggestion(p.PartName, p.AccuracyPercent),
                    ActionUrl = $"/practice?part={Uri.EscapeDataString(p.PartName)}",
                })
                .ToList();

            // 7. Tổng hợp
            var latest = attempts.First();
            var prev = attempts.Skip(1).FirstOrDefault();
            var latestPct = latest.TotalQuestions > 0
                ? Math.Round((double)(latest.TotalScore ?? 0) / latest.TotalQuestions * 100, 1) : 0;
            var prevPct = prev != null && prev.TotalQuestions > 0
                ? Math.Round((double)(prev.TotalScore ?? 0) / prev.TotalQuestions * 100, 1) : 0;

            return new ExamAnalyticsDto
            {
                TotalAttempts = await _context.ExamAttempts
                    .CountAsync(a => a.UserId == request.UserId
                              && a.Status == Domain.Entities.ExamAttemptStatus.Submitted
                              && !a.IsDeleted, ct),
                AverageScore = scoreHistory.Count > 0
                    ? Math.Round(scoreHistory.Average(s => s.Percent), 1) : 0,
                BestScore = scoreHistory.Count > 0
                    ? scoreHistory.Max(s => s.Percent) : 0,
                LatestScore = latestPct,
                ScoreTrend = Math.Round(latestPct - prevPct, 1),
                PartAnalytics = partGroups,
                Strengths = strengths,
                Weaknesses = weaknesses,
                Suggestions = suggestions,
                ScoreHistory = scoreHistory,
            };
        }

        private static string GetSkill(string partName) =>
            partName is "Part 1" or "Part 2" or "Part 3" or "Part 4"
                ? "Listening" : "Reading";

        private static string BuildSuggestion(string part, double accuracy) =>
            part switch
            {
                "Part 1" => $"Độ chính xác {accuracy}% — Luyện nhận diện hình ảnh và nghe mô tả.",
                "Part 2" => $"Độ chính xác {accuracy}% — Luyện nghe câu hỏi ngắn và chọn câu trả lời phù hợp.",
                "Part 3" => $"Độ chính xác {accuracy}% — Luyện nghe hội thoại và xác định ý chính.",
                "Part 4" => $"Độ chính xác {accuracy}% — Luyện nghe bài nói đơn và ghi chú thông tin.",
                "Part 5" => $"Độ chính xác {accuracy}% — Ôn ngữ pháp và từ vựng điền vào chỗ trống.",
                "Part 6" => $"Độ chính xác {accuracy}% — Luyện đọc đoạn văn và điền từ phù hợp.",
                "Part 7" => $"Độ chính xác {accuracy}% — Luyện đọc hiểu đa văn bản và xác định thông tin.",
                _ => $"Độ chính xác {accuracy}% — Cần luyện tập thêm.",
            };
    }
}