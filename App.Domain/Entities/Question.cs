
namespace App.Domain.Entities
{
    // entity câu hỏi
    public class Question : BaseEntity
    {
        // --- PHÂN LOẠI ---
        public Guid CategoryId { get; set; }           // Part, Topic, Lesson
        public Guid? GroupId { get; set; }             // Bài đọc, bài nghe (Passage)

        // SingleChoice, MultipleChoice, FillBlank, Matching, Essay, Speaking
        public string QuestionType { get; set; }
        public PromptType? PromptTypes { get; set; } // Thêm enum cho Writing (Task1/Task2) hoặc Speaking (Part1/Part2/Part3)

        public bool IsActive { get; set; } = true;
        public bool IsPublic { get; set; } = true;     // Để xuất hiện trong ngân hàng chung

        public Guid? DifficultyId { get; set; }

        public virtual Category? Difficulty { get; set; }

        // --- NỘI DUNG ---
        public string? Content { get; set; }            // HTML content
        public string? Explanation { get; set; }       // Giải thích khi xem kết quả

        // --- THỜI GIAN ---
        public int? TimeLimitSeconds { get; set; }     // Nếu câu bị giới hạn thời gian

        // --- CHẤM ĐIỂM ---
        public bool ShuffleAnswers { get; set; } = true;
        public double DefaultScore { get; set; } = 1.0;

        public int? MinWords { get; set; } // Thêm cho Writing/Speaking (business: enforce word limit)
        public int? MaxWords { get; set; }
        public string? RubricJson { get; set; } // Thêm JSON cho grading criteria (e.g., IELTS band descriptors)

        // --- QUAN HỆ ---    // Mối quan hệ: Một Group có nhiều câu hỏi con
        public virtual Category Category { get; set; }
        public virtual QuestionGroup? Group { get; set; }
        public virtual ICollection<Answer> Answers { get; set; }
        public virtual ICollection<QuestionMedia> Media { get; set; }
        public virtual ICollection<QuestionTag> Tags { get; set; }

        public enum PromptType // Enum mới thêm cho business (IELTS-specific)
        {
            WritingTask1 = 1, // e.g., Graph/Letter
            WritingTask2 = 2, // Essay
            SpeakingPart1 = 3, // Short answers
            SpeakingPart2 = 4, // Cue card
            SpeakingPart3 = 5 // Discussion
        }
    }
}
