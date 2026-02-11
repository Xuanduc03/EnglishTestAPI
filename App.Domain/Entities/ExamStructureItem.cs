
namespace App.Domain.Entities
{
    public class ExamStructureItem : BaseEntity
    {
        public Guid ExamId { get; set; }

        public string PartName { get; set; }           // Part 1 / Section A...
        public int OrderIndex { get; set; }            // Thứ tự hiển thị

        // SỐ LƯỢNG CÂU
        public int QuestionCount { get; set; }

        // NGUỒN CÂU HỎI
        public Guid SourceCategoryId { get; set; }

        // MODE LẤY CÂU
        public string SelectionMode { get; set; } = "Random";
        // Random / Fixed / DifficultyBased

        // ĐỘ KHÓ TÙY CHỌN
        public int? MinDifficulty { get; set; }
        public int? MaxDifficulty { get; set; }

        // ĐIỂM
        public double ScorePerQuestion { get; set; } = 1.0;
    }

}
