using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    // ── Shared filter ────────────────────────────────────────
    public record DateRangeFilter(
        DateTime? From,
        DateTime? To,
        Granularity Granularity = Granularity.Day
    );

    public enum Granularity { Day, Week, Month }

    // ── Dashboard overview ───────────────────────────────────
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int TotalExams { get; set; }
        public int TotalAttempts { get; set; }
        public int AttemptsToday { get; set; }
        public double AverageScore { get; set; }  // 0-990 TOEIC scale
        public double PassRate { get; set; }  // 0-1
        public double AvgCompletionMins { get; set; }
        // % thay đổi so với kỳ trước (dùng cho badge +/-%)
        public double UserGrowthPct { get; set; }
        public double AttemptGrowthPct { get; set; }
        public double ScoreGrowthPct { get; set; }
    }

    // ── Time series ──────────────────────────────────────────
    public class TimeSeriesDataPointDto
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty; // "Th1", "T2"...
        public double Value { get; set; }
    }

    public class UserGrowthDto
    {
        public List<TimeSeriesDataPointDto> NewUsers { get; set; } = new();
        public List<TimeSeriesDataPointDto> TotalUsers { get; set; } = new();
        public int TotalNewUsers { get; set; }
    }

    public class ExamSubmissionStatsDto
    {
        public List<TimeSeriesDataPointDto> Submissions { get; set; } = new();
        public List<TimeSeriesDataPointDto> Completions { get; set; } = new();
        public int TotalSubmitted { get; set; }
        public double AvgScore { get; set; }
    }

    // ── Score distribution ───────────────────────────────────
    public class ScoreBucketDto
    {
        public string Label { get; set; } = string.Empty; // "0-100", "100-200"...
        public int Min { get; set; }
        public int Max { get; set; }
        public int Count { get; set; }
        public double Pct { get; set; } // % tổng
    }

    public class ScoreDistributionDto
    {
        public List<ScoreBucketDto> Buckets { get; set; } = new();
        public double Mean { get; set; }
        public double Median { get; set; }
        public double StdDev { get; set; }
        public int TotalAttempts { get; set; }
    }

    // ── Pass rate ────────────────────────────────────────────
    public class PassRateDto
    {
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public double PassRate { get; set; } // 0-1
        public double AvgScore { get; set; }
    }

    // ── Completion time ──────────────────────────────────────
    public class CompletionTimeDto
    {
        public double AvgMinutes { get; set; }
        public double MedianMinutes { get; set; }
        public double P90Minutes { get; set; } // 90th percentile
        public List<TimeSeriesDataPointDto> ByDay { get; set; } = new();
    }

    // ── Top exams / users ────────────────────────────────────
    public class TopExamDto
    {
        public Guid ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public double AvgScore { get; set; }
        public double PassRate { get; set; }
    }

    public class TopUserDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int AttemptCount { get; set; }
        public double AvgScore { get; set; }
        public double BestScore { get; set; }
    }

    // ── Skill stats (Part 1-7) ───────────────────────────────
    public class SkillStatsDto
    {
        public string PartName { get; set; } = string.Empty; // "Part 1"
        public string SkillType { get; set; } = string.Empty; // "Listening"
        public double AvgScore { get; set; }
        public double AvgAccuracy { get; set; } // % đúng
        public int TotalQuestions { get; set; }
        public int TotalAnswered { get; set; }
    }

    // ── Recent activity ──────────────────────────────────────
    public class RecentActivityDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "submitted", "registered"
        public string ExamTitle { get; set; } = string.Empty;
        public double? Score { get; set; }
        public DateTime OccuredAt { get; set; }
    }
}
