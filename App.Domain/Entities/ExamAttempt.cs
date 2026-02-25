using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    /// <summary>
    /// Lưu trữ phiên thi của user khi làm bài test
    /// </summary>
    public class ExamAttempt : BaseEntity
    {
        // who & what
        public Guid UserId { get; set; }
        public Guid ExamId { get; set; }

        // timing
        public DateTime StartedAt { get; set; } // thời gian bắt đầu 
        public DateTime? SubmitedAt { get; set; } // thời gian nạp bài
        public int TimeLimitSeconds { get; set; }   // Copy từ Exam.Duration khi bắt đầu
        public DateTime? ExpiresAt { get; set; }     // StartedAt + TimeLimitSeconds → Auto-submit

        // status
        public ExamAttemptStatus Status { get; set; } = ExamAttemptStatus.InProgress;
        public int? ActualTimeSeconds { get; set; }

        // scoring
        public int? ListeningCorrect { get; set; }
        public int? ReadingCorrect { get; set; }
        public int? ListeningScore { get; set; } // 0 - 495 
        public int? ReadingScore { get; set; } // 0 -495
        public int? TotalScore { get; set; } // 0 - 990

        //summary
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int UnanswerQuestions { get; set; } // Những câu hỏi chưa được giải đáp

        // resume support
        public int? LastAnsweredIndex { get; set; } // câu làm cuối cùng 
        public string? ProgressSnapshot { get; set; } // JSON snapshot

        // Anti-cheat tracking
        public string IpAddress { get; set; } // Địa chỉ IP của thiết bị mà thí sinh đang dùng để truy cập bài thi
        public string UserAgent { get; set; }  // Chuỗi thông tin mà trình duyệt gửi lên server mỗi khi request
        public List<string> AntiCheatFlags { get; set; } = new(); // Flagged reasons
        public int TabSwitchCount { get; set; } = 0; // Số lần thí sinh chuyển tab
        public int PageReloadCount { get; set; } = 0; // Số lần thí sinh reload/trở lại trang

        // audit 
        public byte[] VersionNumber { get; set; }

        // === NAVIGATION ===
        public virtual User User { get; set; }
        public virtual Exam Exam { get; set; }
        public virtual ICollection<ExamAnswer> Answers { get; set; }
        public virtual ICollection<ExamSectionResult> SectionResults { get; set; }
    }

    public enum ExamAttemptStatus
    {
        InProgress = 0,    // Đang làm
        Submitted = 1,    // Nộp bài thủ công
        TimedOut = 2,    // Hết giờ auto-submit
        Abandoned = 3,    // Thoát giữa chừng
    }
}
