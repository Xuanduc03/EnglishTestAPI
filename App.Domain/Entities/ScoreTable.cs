

namespace App.Domain.Entities
{
    public class ScoreTable : BaseEntity
    {
        public Guid ExamId { get; set; }
        public virtual Exam Exam { get; set; }

        // Loại kỹ năng áp dụng: "Listening" hoặc "Reading"
        public Guid? CategoryId { get; set; }
        public virtual Category Category { get;   set; }

        // JSON chứa quy tắc quy đổi
        // VD: {"10": 50, "11": 55, ..., "90": 450}
        // Key: Số câu đúng, Value: Điểm số
        public string ConversionJson { get; set; }
    }
}
