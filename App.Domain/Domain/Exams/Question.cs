
using App.Domain.Domain.Exams;

namespace App.Domain.Entities
{
    /// <summary>
    /// Entity: Lưu trữ dữ liệu của câu hỏi đơn, câu hỏi con trong câu hỏi nhóm
    /// </summary>
    public class Question : BaseEntity
    {
        // --- PHÂN LOẠI ---
        public Guid CategoryId { get; set; }           // Part, Topic, Lesson
        public Guid? GroupId { get; set; }             // Bài đọc, bài nghe (Passage)

        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true;     // Để xuất hiện trong ngân hàng chung

        public Guid? DifficultyId { get; set; }
        public int OrderIndex { get; set; } // Thứ tự câu hỏi trong nhóm (dành cho Part 3,4,6,7)
        public virtual Category? Difficulty { get; set; }

        // --- NỘI DUNG ---
        public string? Content { get; set; }            // HTML content
        public string? Explanation { get; set; }       // Giải thích khi xem kết quả
        public string? MetadataJson { get; set; }           // Setting linh hoạt

        // --- THỜI GIAN ---
        public int? TimeLimitSeconds { get; set; }     // Nếu câu bị giới hạn thời gian

        // ── ĐÁP ÁN & AI GRADING : dùng mẫu cho ai chấm ───────────────────────
        public bool ShuffleAnswers { get; set; } = true;
        public double DefaultScore { get; set; } = 1.0;

        public bool IsAiGraded { get; set; } = false;       // Bật AI chấm
        public string? RubricJson { get; set; } // Thêm JSON cho grading criteria (e.g., IELTS band descriptors) 
        public string? AiPromptTemplate { get; set; }
       
        public int? MinWords { get; set; } // Thêm cho Writing/Speaking (business: enforce word limit)
        public int? MaxWords { get; set; }

        // ── THÊM CHO WRITING TOEIC ───────────────────────
        public string? SampleAnswer { get; set; }           // Đáp án mẫu

        // --- QUAN HỆ ---    // Mối quan hệ: Một Group có nhiều câu hỏi con
        public virtual Category Category { get; set; }
        public virtual QuestionGroup? Group { get; set; }
        public virtual ICollection<Answer> Answers { get; set; }
        public virtual ICollection<QuestionMedia> Media { get; set; }
        public virtual ICollection<QuestionTag> Tags { get; set; }


        //ENUM MAPPING ───────────────────────

        /// <summary>Lưu xuống DB (Bắt buộc)</summary>
        public int QuestionTypeId { get; set; }

        /// <summary>Dùng trong code C# (Bỏ qua khi map DB)</summary>
        public QuestionType QuestionType
        {
            get => (QuestionType)QuestionTypeId;
            set => QuestionTypeId = (int)value;
        }

        /// <summary>Lưu xuống DB (Nullable - vì có câu không cần prompt AI)</summary>
        public int? PromptTypeId { get; set; }

        /// <summary>Dùng trong code C# (Bỏ qua khi map DB)</summary>
        public PromptType? PromptType
        {
            get => PromptTypeId.HasValue ? (PromptType)PromptTypeId.Value : null;
            set => PromptTypeId = value.HasValue ? (int)value.Value : null;
        }

    }
}
