
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

        // SingleChoice, MultipleChoice, FillBlank, Matching, Essay, Speaking
        public QuestionTypeEnum QuestionType { get; set; }
        public PromptTypeEnum? PromptTypes { get; set; } // Thêm enum cho Writing (Task1/Task2) hoặc Speaking (Part1/Part2/Part3)

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
        public int? ActualWordCount { get; set; }           // Số từ thực tế của học viên

        // --- QUAN HỆ ---    // Mối quan hệ: Một Group có nhiều câu hỏi con
        public virtual Category Category { get; set; }
        public virtual QuestionGroup? Group { get; set; }
        public virtual ICollection<Answer> Answers { get; set; }
        public virtual ICollection<QuestionMedia> Media { get; set; }
        public virtual ICollection<QuestionTag> Tags { get; set; }

     }

    // ── ENUMS ─────────────────────────────────────────────
    // ── QuestionTypeEnum ──────────────────────────────────
    public enum QuestionTypeEnum
    {
        // ── TOEIC ─────────────────────────────────────────
        SingleChoice = 1,    // Part 1,2,3,4,5,6,7 — MCQ 4 đáp án
        MultipleChoice = 2,    // Chọn nhiều đáp án
        FillBlank = 3,    // Part 5,6 — điền từ vào chỗ trống

        // ── MATCHING (IELTS Reading/Listening) ────────────
        Matching = 4,    // Nối thông tin chung
        MatchingHeading = 5,    // Nối tiêu đề đoạn văn
        MatchingInformation = 6,    // Nối thông tin vào đoạn

        // ── SHORT ANSWER (IELTS) ──────────────────────────
        ShortAnswer = 7,    // Trả lời ngắn (≤3 từ)
        NoteCompletion = 8,    // Điền vào ghi chú
        FormCompletion = 9,    // Điền vào form/bảng
        MapLabeling = 10,   // Điền nhãn bản đồ/sơ đồ
        SentenceCompletion = 11, // Hoàn thành câu

        // ── TRUE/FALSE/NOT GIVEN (IELTS Reading) ──────────
        TrueFalseNotGiven = 12,   // True / False / Not Given
        YesNoNotGiven = 13,   // Yes / No / Not Given (quan điểm tác giả)

        // ── WRITING ───────────────────────────────────────
        Writing = 14,   // Tự luận — AI chấm

        // ── SPEAKING ─────────────────────────────────────
        Speaking = 15,   // Nói — AI chấm
    }

    // ── PromptTypeEnum ────────────────────────────────────
    public enum PromptTypeEnum
    {
        // ── IELTS Writing ─────────────────────────────────
        IeltsWritingTask1Graph = 1,    // Mô tả biểu đồ (bar, line, pie...)
        IeltsWritingTask1Letter = 2,    // Viết thư (General Training)
        IeltsWritingTask1Process = 3,    // Mô tả quy trình
        IeltsWritingTask1Map = 4,    // Mô tả bản đồ
        IeltsWritingTask2Opinion = 5,    // Trình bày quan điểm
        IeltsWritingTask2Discussion = 6,    // Thảo luận 2 quan điểm
        IeltsWritingTask2Problem = 7,    // Vấn đề + giải pháp
        IeltsWritingTask2Advantage = 8,    // Lợi ích + bất lợi

        // ── IELTS Speaking ────────────────────────────────
        IeltsSpeakingPart1 = 9,    // Câu hỏi về bản thân/đời thường
        IeltsSpeakingPart2 = 10,   // Cue card — nói 1-2 phút
        IeltsSpeakingPart3 = 11,   // Thảo luận chuyên sâu

        // ── TOEIC Writing ─────────────────────────────────
        ToeicPictureDescription = 12,   // Viết câu mô tả tranh
        ToeicWritingEmailResponse = 13,   // Trả lời email
        ToeicWritingOpinionEssay = 14,   // Viết luận quan điểm

        // ── TOEIC Speaking ────────────────────────────────
        ToeicSpeakingReadAloud = 15,   // Đọc to đoạn văn
        ToeicSpeakingDescribePicture = 16,  // Mô tả tranh
        ToeicSpeakingQuestions = 17,   // Trả lời câu hỏi
        ToeicSpeakingProposeSolution = 18,  // Đề xuất giải pháp
        ToeicSpeakingOpinion = 19,   // Trình bày ý kiến
    }
}
