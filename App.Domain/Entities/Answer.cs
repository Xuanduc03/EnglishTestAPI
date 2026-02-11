
namespace App.Domain.Entities
{
    public class Answer : BaseEntity
    {
        public Guid QuestionId { get; set; }

        public string Content { get; set; }            // Nội dung đáp án
        public bool IsCorrect { get; set; }            // Đáp án đúng
        public int OrderIndex { get; set; }            // Thứ tự hiển thị

        // GIẢI THÍCH CHO TỪNG ĐÁP ÁN
        public string? Feedback { get; set; }

        // Chấm điểm partial (multiple correct)
        public string? Explanation { get; set; } // Thêm để feedback chi tiết per answer (business: giúp học viên học)
        public double? ScoreWeight { get; set; }
    }
}
