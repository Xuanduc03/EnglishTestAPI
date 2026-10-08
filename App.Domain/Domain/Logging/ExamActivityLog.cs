using App.Domain.Entities;

namespace App.Domain.Domain.Logging
{
    /// <summary>
    /// Lưu lịch sử hoạt động của người dùng trong một lượt thi.
    /// </summary>
    public class ExamActivityLog : BaseEntity
    {
        /// <summary>
        /// Lượt thi mà activity này thuộc về.
        /// </summary>
        public Guid ExamAttemptId { get; set; }

        /// <summary>
        /// Người thực hiện hành động.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Loại hành động trong quá trình thi.
        /// </summary>
        public ExamActivityAction Action { get; set; }

        /// <summary>
        /// Câu hỏi liên quan, nếu có.
        /// </summary>
        public Guid? QuestionId { get; set; }

        /// <summary>
        /// Thời điểm activity xảy ra.
        /// </summary>
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Số giây đã trôi qua kể từ khi bắt đầu lượt thi.
        /// </summary>
        public int? ElapsedSeconds { get; set; }

        /// <summary>
        /// Thông tin bổ sung của activity.
        /// Ví dụ: answer cũ, answer mới, vị trí câu hỏi...
        /// </summary>
        public string? MetadataJson { get; set; }

        /// <summary>
        /// Địa chỉ IP của người thực hiện.
        /// </summary>
        public string? IpAddress { get; set; }

        public virtual ExamAttempt ExamAttempt { get; set; } = null!;

        public virtual User User { get; set; } = null!;

        public virtual Question? Question { get; set; }
    }
}
