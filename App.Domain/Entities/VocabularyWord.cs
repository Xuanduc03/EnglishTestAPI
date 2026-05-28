
namespace App.Domain.Entities
{
    /// <summary>
    /// Entitt: Lưu trữ dữ liệu từ vựng
    /// </summary>
    public class VocabularyWord : BaseEntity
    {
        public string Word { get; set; }               // Từ vựng
        public string PartOfSpeech { get; set; }       // Loại từ (adj, v, n,...)
        public string Phonetic { get; set; }            // Phiên âm
        public string Meaning { get; set; }             // Nghĩa của từ
        public int OrderIndex { get; set; }             // STT (có thể dùng để sắp xếp)
        public string? AudioUrl { get; set; }      // Phát âm
        public string? AudioPublicId { get; set; }
        public string? ImageUrl { get; set; }      // Hình ảnh minh họa
        public string? ImagePublicId { get; set; }
        public string? Example { get; set; }       // Câu ví dụ
        public string? ExampleMeaning { get; set; } // Nghĩa câu ví dụ
        public string? Level { get; set; }         // A1, A2, B1...
        public Guid? CategoryId { get; set; }      // Topic (Business, Travel...)
        public virtual Category? Category { get; set; }

        // Liên kết đến tiến độ học của người dùng
        public virtual ICollection<UserVocabularyProgress> Progresses { get; set; }
    }
}
