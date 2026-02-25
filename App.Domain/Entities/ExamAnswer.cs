

namespace App.Domain.Entities
{
    /// <summary>
    /// Đại diện cho câu trả lời của thí sinh cho từng câu hỏi trong một lần thi (ExamAttempt).
    /// Lưu trữ thông tin chi tiết về lựa chọn của người dùng, thời gian làm bài, điểm số và trạng thái đúng/sai.
    /// </summary>
    public class ExamAnswer : BaseEntity
    {
        /// <summary>
        /// Khóa ngoại liên kết với lần thi (ExamAttempt) mà câu trả lời này thuộc về.
        /// Bắt buộc, không null.
        /// </summary>
        public Guid ExamAttemptId { get; set; }

        /// <summary>
        /// Khóa ngoại tham chiếu đến bản ghi trung gian ExamQuestion (bảng nối giữa Exam và Question).
        /// Dùng để biết câu hỏi này nằm ở vị trí nào trong đề thi cụ thể.
        /// Bắt buộc, không null.
        /// </summary>
        public Guid ExamQuestionId { get; set; }

        /// <summary>
        /// Khóa ngoại tham chiếu đến câu hỏi gốc (Question) trong ngân hàng câu hỏi.
        /// Dùng để lấy nội dung câu hỏi, đáp án đúng, loại câu hỏi khi cần chấm điểm hoặc hiển thị giải thích.
        /// Bắt buộc, không null.
        /// </summary>
        public Guid QuestionId { get; set; }

        /// <summary>
        /// ID của đáp án mà thí sinh đã chọn (nếu là câu trắc nghiệm).
        /// NULL nếu: 
        /// - Thí sinh chưa trả lời câu này
        /// - Câu hỏi dạng tự luận (không có đáp án cố định)
        /// </summary>
        public Guid? SelectedAnswerId { get; set; }

        /// <summary>
        /// Trạng thái câu hỏi đã được trả lời hay chưa.
        /// true: Thí sinh đã submit đáp án (dù đúng hay sai).
        /// false: Chưa trả lời hoặc bỏ qua.
        /// </summary>
        public bool IsAnswered { get; set; }

        /// <summary>
        /// Kết quả chấm điểm: câu trả lời có đúng không.
        /// true: Đúng
        /// false: Sai
        /// Giá trị chỉ được cập nhật sau khi nộp bài (SubmitExam).
        /// </summary>
        public bool IsCorrect { get; set; }

        /// <summary>
        /// Điểm số của câu trả lời này (sau khi chấm).
        /// Giá trị lấy từ ExamQuestion.Point (điểm tối đa của câu hỏi).
        /// Nếu sai = 0, đúng = Point.
        /// Hỗ trợ câu hỏi có điểm lẻ (ví dụ: 1.25, 2.5).
        /// </summary>
        public double Point { get; set; }

        /// <summary>
        /// Thời gian thí sinh dành để trả lời câu hỏi này (tính bằng giây).
        /// NULL nếu chưa trả lời hoặc không track thời gian chi tiết.
        /// Dùng để phân tích hành vi làm bài (ví dụ: câu nào làm quá nhanh → nghi ngờ cheat).
        /// </summary>
        public int? TimeSpentSeconds { get; set; }

        /// <summary>
        /// Thời điểm thí sinh trả lời câu hỏi này (server time, UTC).
        /// NULL nếu chưa trả lời.
        /// Dùng để tính thứ tự trả lời, phát hiện hành vi bất thường (ví dụ: trả lời hàng loạt cùng lúc).
        /// </summary>
        public DateTime? AnsweredAt { get; set; }
        public int VersionNumber { get; set; }

        /// <summary>
        /// Navigation property: Lần thi mà câu trả lời này thuộc về.
        /// </summary>
        public virtual ExamAttempt Attempt { get; set; }

        /// <summary>
        /// Navigation property: Câu hỏi gốc trong ngân hàng câu hỏi.
        /// Dùng để lấy đáp án đúng, nội dung, loại câu hỏi khi chấm hoặc hiển thị kết quả.
        /// </summary>
        public virtual ExamQuestion ExamQuestions { get; set; }
        public Guid? CorrectAnswerId { get; set; }
    }
}