

using System.ComponentModel;

namespace App.Domain.Entities
{
    // Entity thể hiện 1 bài thi    
    public class Exam : BaseEntity
    {
        // Thông tin cơ bản
        public string Title { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }

        // Thời gian & điểm số
        public int Duration { get; set; }
        public decimal TotalScore { get; set; }

        // ========== PHÂN LOẠI ĐỀ THI ==========

        // Loại kỳ thi (TOEIC, IELTS...)
        public ExamType Type { get; set; }

        // Loại đề thi (Full Test, Practice, Mini Test...)
        public ExamCategory Category { get; set; }

        // Phạm vi bài thi (Full, Listening Only, Part 5 Only...)
        public ExamScope Scope { get; set; }

        // Mức độ (Mock Test, Practice, Real Exam...)
        public ExamLevel Level { get; set; }

        // ========== TAGS & METADATA ==========

        // Tags linh hoạt: ["Part5", "Part6", "Grammar", "Beginner"]
        public string? Tags { get; set; } // JSON array

        // Metadata mở rộng (JSON)
        // VD: {"targetScore": 700, "difficulty": "Medium", "fokus": "Grammar"}
        public string? MetaData { get; set; }

        // ========== TRẠNG THÁI & CÀI ĐẶT ==========

        public ExamStatus Status { get; set; }
        public bool ShuffleQuestions { get; set; }
        public bool ShuffleAnswers { get; set; }
        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;

        // Relations
        public virtual ICollection<ExamSection> Sections { get; set; }
        public virtual ICollection<ScoreTable> ScoreTables { get; set; }
    }

    public enum ExamStatus
    {
        // 0. Đang soạn thảo
        // Admin đang nhập câu hỏi, chưa hoàn thiện.
        // Học viên KHÔNG thấy đề này.
        [Description("Bản nháp")]
        Draft = 0,

        // 1. Chờ duyệt (Optional - Dùng nếu hệ thống có người kiểm duyệt)
        // Giáo viên soạn xong, gửi lên Admin duyệt.
        [Description("Chờ duyệt")]
        PendingReview = 1,

        // 2. Đã xuất bản / Công khai
        // Đề thi đã hoàn chỉnh. Học viên có thể nhìn thấy và vào thi.
        [Description("Đã xuất bản")]
        Published = 2,

        // 3. Tạm ngưng
        // Đề thi bị phát hiện lỗi hoặc tạm khóa để bảo trì.
        // Học viên không thể vào thi mới, nhưng kết quả cũ vẫn còn.
        [Description("Tạm ngưng")]
        Suspended = 3,

        // 4. Lưu trữ
        // Đề thi cũ (ví dụ bộ đề năm 2018).
        // Ẩn khỏi danh sách tìm kiếm mặc định, chỉ dùng để tra cứu lịch sử.
        [Description("Lưu trữ")]
        Archived = 99
    }



    // ============================================
    // ENUM: LOẠI KỲ THI
    // ============================================
    public enum ExamType
    {
        TOEIC = 1,
        IELTS = 2,
        TOEFL = 3,
        SAT = 4,
        Other = 99
    }

    // ============================================
    // ENUM: LOẠI ĐỀ THI (MỚI)
    // ============================================
    public enum ExamCategory
    {
        FullTest = 1,           // Đề thi đầy đủ (200 câu TOEIC)
        SkillTest = 2,          // Luyện 1 kỹ năng (Listening/Reading/Writing/Speaking)
        PartTest = 3,           // Luyện 1 Part (Part 5, Part 7...)
        MiniTest = 4,           // Mini test (Kết hợp 2-3 parts)
        DiagnosticTest = 5,     // Đề kiểm tra trình độ
        AssignmentTest = 6      // Bài tập về nhà
    }

    // ============================================
    // ENUM: PHẠM VI ĐỀ THI (MỚI)
    // ============================================
    public enum ExamScope
    {
        // === FULL TEST ===
        Full = 1,                    // Full test (200 câu TOEIC)

        // === SKILL-BASED ===
        ListeningOnly = 10,          // Chỉ Listening
        ReadingOnly = 11,            // Chỉ Reading
        WritingOnly = 12,            // Chỉ Writing
        SpeakingOnly = 13,           // Chỉ Speaking

        // === TOEIC PARTS ===
        Part1Only = 20,              // TOEIC Part 1 (Photographs)
        Part2Only = 21,              // TOEIC Part 2 (Q&A)
        Part3Only = 22,              // TOEIC Part 3 (Conversations)
        Part4Only = 23,              // TOEIC Part 4 (Talks)
        Part5Only = 24,              // TOEIC Part 5 (Grammar)
        Part6Only = 25,              // TOEIC Part 6 (Text Completion)
        Part7Only = 26,              // TOEIC Part 7 (Reading)

        // === COMBINED PARTS ===
        Part5And6 = 30,              // Part 5 + 6 (Grammar & Text)
        Part3And4 = 31,              // Part 3 + 4 (Listening Long)
        Part1And2 = 32,              // Part 1 + 2 (Listening Short)

        // === IELTS SPECIFIC ===
        IELTSListeningOnly = 40,
        IELTSReadingOnly = 41,
        IELTSWritingTask1 = 42,
        IELTSWritingTask2 = 43,
        IELTSSpeakingPart1 = 44,
        IELTSSpeakingPart2 = 45,
        IELTSSpeakingPart3 = 46,

        // === CUSTOM ===
        Custom = 99                  // Tùy chỉnh (admin tự định nghĩa)
    }

    // ============================================
    // ENUM: MỨC ĐỘ ĐỀ THI (MỚI)
    // ============================================
    public enum ExamLevel
    {
        Practice = 1,        // Luyện tập hàng ngày
        MockTest = 2,        // Thi thử (giống thi thật)
        Assignment = 3,      // Bài tập về nhà
        MidTerm = 4,         // Thi giữa kỳ
        FinalExam = 5,       // Thi cuối kỳ
        Placement = 6,       // Thi phân loại trình độ
        RealExam = 7         // Thi thật (Official)
    }

}
