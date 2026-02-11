using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
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
