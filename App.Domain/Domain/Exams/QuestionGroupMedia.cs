
namespace App.Domain.Entities
{
    /// <summary>
    /// Enity: Lưu trữ media của câu hỏi nhóm
    /// </summary>
    public class QuestionGroupMedia : BaseEntity
    {
        public Guid QuestionGroupId { get; set; }
        public string Url { get; set; }
        public string PublicId { get; set; }
        public string? FileHash { get; set; } // hash file để check độ tương đồng 
        public string MediaType { get; set; }  // "audio" / "image"
        public int OrderIndex { get; set; } = 1;

        public virtual QuestionGroup QuestionGroup { get; set; }
    }
}
