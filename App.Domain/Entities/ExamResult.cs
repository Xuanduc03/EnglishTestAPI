

namespace App.Domain.Entities
{
    public class ExamResult : BaseEntity
    {
        public Guid StudentId { get; set; } // Người thi
        public Guid ExamId { get; set; }    // Đề thi nào
        public virtual Student Student { get; set; }

        public virtual Exam Exam { get; set; }

        // Thời gian bắt đầu - kết thúc
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Tổng điểm đạt được
        public double TotalScore { get; set; }

        // Chi tiết điểm từng phần (Lưu JSON cho gọn)
        // VD: {"Listening": 300, "Reading": 250}
        public string? ScoreDetailJson { get; set; }

        // Tổng số câu đúng / Tổng số câu
        public int CorrectCount { get; set; }
        public int TotalQuestion { get; set; }

        // Trạng thái: InProgress (Đang làm), Completed (Nộp bài)
        public string Status { get; set; }

        // Chi tiết từng câu trả lời
        //public virtual ICollection<StudentAnswer> StudentAnswers { get; set; }
    }
}
