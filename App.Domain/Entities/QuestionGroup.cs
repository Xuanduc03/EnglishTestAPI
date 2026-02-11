

using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Entities
{
    public class QuestionGroup :BaseEntity
    {
        // Nội dung đoạn văn, hội thoại (HTML/Text)
        // Nếu là bài nghe thì có thể để trống hoặc chứa Transcript
        // --- 2. PHÂN LOẠI (Theo chuẩn mới) ---
        public Guid CategoryId { get; set; } // Thuộc Part nào (Part 6, Part 7, Listening Sec 3...)
        public Guid? DifficultyId { get; set; } // Độ khó của bài đọc/nghe
        public virtual Category? Difficulty { get; set; }
        public string? Content { get; set; } // nội dung câu hỏi
        public string? Explanation { get; set; }    // Giải thích khi xem kết quả
        public string? Transcript { get; set; }   // Lời thoại (Hiện ra sau khi thi xong để học sinh đối chiếu)

        // JSON lưu thông tin phụ (Ví dụ: Nguồn bài báo, Tác giả...)
        public string? MediaJson { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Category Category { get; set; }
        public virtual ICollection<QuestionGroupMedia> Media { get; set; } = new List<QuestionGroupMedia>();

        // Danh sách câu hỏi con (Ví dụ: 5 câu hỏi ăn theo bài đọc này)
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<QuestionTag> Tags { get; set; }
    }
}
