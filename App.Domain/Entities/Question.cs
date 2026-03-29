
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
        SingleChoice = 1,   // Part 1/2/3/4/5/6/7
        MultipleChoice = 2,   // Chọn nhiều đáp án
        FillBlank = 3,   // Part 5/6 — 4 đáp án A/B/C/D

        // ── IELTS Matching ────────────────────────────────
        Matching = 4,   // Matching Features
        MatchingHeading = 5,   // Matching Headings (i, ii, iii...)
        MatchingInformation = 6,   // Matching Information (A, B, C...)
        MatchingSentenceEnds = 7,   // Matching Sentence Endings  ← thêm

        // ── IELTS True/False/Not Given ─────────────────────
        TrueFalseNotGiven = 8,   // TRUE / FALSE / NOT GIVEN  ← đổi số
        YesNoNotGiven = 9,   // YES / NO / NOT GIVEN      ← đổi số

        // ── IELTS + TOEIC Completion ──────────────────────
        // Chấm bằng string.Equals(userInput, Answer.Content, OrdinalIgnoreCase)
        ShortAnswer = 10,  // Trả lời ngắn ≤3 từ        ← đổi số
        NoteCompletion = 11,  // Điền vào ghi chú          ← đổi số
        FormCompletion = 12,  // Điền vào form             ← đổi số
        TableCompletion = 13,  // Điền vào bảng             ← thêm
        SummaryCompletion = 14,  // Điền vào đoạn tóm tắt     ← thêm
        SentenceCompletion = 15,  // Hoàn thành câu            ← đổi số
        MapLabeling = 16,  // Điền nhãn bản đồ/sơ đồ   ← đổi số
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
