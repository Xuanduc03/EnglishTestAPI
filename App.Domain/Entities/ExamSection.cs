

namespace App.Domain.Entities
{
    public class ExamSection : BaseEntity
    {
        public Guid ExamId { get; set; }
        public virtual Exam Exam { get; set; }

        // Mô tả hướng dẫn bài làm cho phần này 
        public string? Instructions { get; set; }

        public Guid CategoryId { get; set; }
        public Category Category { get; set; }
        // thứ tự : 1
        public int OrderIndex { get; set; }
        public int? TimeLimit { get; set; } // Thêm phút per section (business: e.g., Listening 30 mins)
        // Liên kết mapping câu hỏi vào phần này
        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; }
    }
}
