
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

        // Liên kết đến tiến độ học của người dùng
        public virtual ICollection<UserVocabularyProgress> Progresses { get; set; }
    }
}
