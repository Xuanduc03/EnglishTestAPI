

namespace App.Domain.Entities
{
    /// <summary>
    /// Kết quả từng section
    /// </summary>
    public  class ExamSectionResult : BaseEntity
    {
        public Guid ExamAttemptId { get; set; }
        public Guid ExamSectionId { get; set; }

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int? ConvertedScore { get; set; }    // Sau khi lookup ScoreTable

        public virtual ExamAttempt Attempt { get; set; }
        public virtual ExamSection Section { get; set; }
    }
}
