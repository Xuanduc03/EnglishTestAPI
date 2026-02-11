

namespace App.Domain.Entities
{
    public class ExamQuestion : BaseEntity
    {
        // đề thi
        public Guid ExamId { get; set; }
        public virtual Exam Exam { get; set; }
        
        // phần thi
        public Guid ExamSectionId { get; set; }
        public virtual ExamSection ExamSection { get; set; }

        // Link tới câu hỏi gốc trong kho
        public Guid QuestionId { get; set; }
        public virtual Question Question { get; set; }

        // Đánh số thứ tự câu hỏi
        public int QuestionNo { get; set; }

        // Điểm số riệng cho câu này trong đề này (Override)
        // Ví dụ: Câu này trong đề A là 5 điểm, trong đề B là 10 điểm
        public decimal Point { get; set; }

        // Thứ tự câu trong đề thi (Câu 1 đến câu 200)
        public int OrderIndex { get; set; }
        // 3. [LOGIC] Cấu hình hành vi
        public bool IsMandatory { get; set; } = true;

        // Có được phép tráo câu này không? 
        // VD: 3 câu hỏi liên quan đến 1 bài đọc thì nên ghim lại gần nhau, ko đảo lộn xộn.
        public bool IsShuffleable { get; set; } = true;
    }
}
