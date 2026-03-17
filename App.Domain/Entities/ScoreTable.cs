

namespace App.Domain.Entities
{
    public class ScoreTable : BaseEntity
    {
        // Trỏ vào Category có code = "LISTENING" hoặc "READING"
        // Dùng chung toàn hệ thống, không phụ thuộc vào đề thi cụ thể
        public Guid SkillCategoryId { get; set; }
        public virtual Category SkillCategory { get; set; }

        // Tên bảng quy đổi: "TOEIC Listening", "TOEIC Reading"
        public string Name { get; set; }

        // Điểm tối thiểu khi 0 câu đúng (chuẩn ETS: 5)
        public int MinScore { get; set; } = 5;

        // Điểm tối đa (chuẩn ETS: 495)
        public int MaxScore { get; set; } = 495;

        public bool IsActive { get; set; } = true;

        // Danh sách các mốc quy đổi: số câu đúng → điểm
        public virtual ICollection<ScoreTableEntry> Entries { get; set; }
            = new List<ScoreTableEntry>();
    }
}
