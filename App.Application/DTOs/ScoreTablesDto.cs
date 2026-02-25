using System.ComponentModel.DataAnnotations;


namespace App.Application.DTOs
{
    // ============================================================
    // DTOs
    // ============================================================
    public class ScoreTableDto
    {
        public Guid Id { get; set; }
        public Guid ExamId { get; set; }
        public string? ExamTitle { get; set; } // Read-only để hiển thị
        public Guid? CategoryId { get; set; }
        // Deserialized để FE dễ dùng: { "10": 50, "90": 450 }
        public Dictionary<string, int> ConversionRules { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ScoreTableListDto
    {
        public Guid Id { get; set; }
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; }
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int TotalRules { get; set; } // Số lượng entries trong ConversionJson
        public DateTime CreatedAt { get; set; }
    }
    // ============================================================
    // SHARED: ConversionRule DTO (1 entry trong bảng quy đổi)
    // ============================================================
    public class ConversionRuleDto
    {
        /// <summary>Số câu đúng (key)</summary>
        [Range(0, 990, ErrorMessage = "Số câu đúng phải từ 0 đến 990")]
        public int CorrectAnswers { get; set; }

        /// <summary>Điểm tương ứng (value)</summary>
        [Range(0, 990, ErrorMessage = "Điểm phải từ 0 đến 990")]
        public int Score { get; set; }
    }
}
