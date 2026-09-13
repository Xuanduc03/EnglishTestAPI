
namespace App.Domain.Entities
{
    /// <summary>
    /// Entity : Lưu trữ media url của câu hỏi đơn
    /// </summary>
    public class QuestionMedia : BaseEntity
    {
        public Guid QuestionId { get; set; }
        public string Url { get; set; }
        public string PublicId { get; set; } // sử dụng để xóa sửa file ở cloudinary
        public string MediaType { get; set; }          // image / audio / video
        public string? FileHash { get; set; } // hash file để check độ tương đồng
        public int OrderIndex { get; set; } = 1; 
    }
}
