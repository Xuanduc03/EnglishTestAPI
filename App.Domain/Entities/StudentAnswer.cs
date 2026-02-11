using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    public class StudentAnswer : BaseEntity
    {
        public Guid ExamResultId { get; set; }
        public virtual ExamResult ExamResult { get; set; }
        public Guid QuestionId { get; set; } // Câu hỏi nào
        public Guid? SelectedAnswerId { get; set; } // Đáp án học sinh chọn (Lưu ID của Answer) Nếu chọn nhiều đáp án thì lưu dạng chuỗi "ID1,ID2" hoặc bảng riêng (nhưng thường TOEIC chỉ chọn 1)
        public string? TextAnswer { get; set; } // Nếu là bài điền từ thì lưu text
        public string? RecordingUrl { get; set; } // Thêm cho Speaking response (business: audio upload)
        public int? WordCount { get; set; } // Thêm auto-calc cho Writing (business: check limit)
        public bool IsCorrect { get; set; } // Câu này đúng hay sai? (Lưu luôn để đỡ phải query lại tính toán)
        public bool AiGraded { get; set; } = false; // Thêm cho auto-score via AI (business: integrate GPT for Writing/Speaking)
        public string? Feedback { get; set; } // Thêm feedback manual/AI (business: learning support)
    }
}
