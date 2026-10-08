using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    public class QuestionTag : BaseEntity
    {
        // --- 1. QUAN HỆ VỚI CÂU HỎI LẺ ---
        public Guid? QuestionId { get; set; } // Để nullable vì có thể tag cho Group

        [ForeignKey("QuestionId")]
        public virtual Question? Question { get; set; }

        // --- 2. QUAN HỆ VỚI BÀI ĐỌC (NÊN CÓ) ---
        // Ví dụ: Bài đọc về chủ đề "Môi trường" -> Tag cả bài luôn
        public Guid? QuestionGroupId { get; set; }

        [ForeignKey("QuestionGroupId")]
        public virtual QuestionGroup? QuestionGroup { get; set; }
        public string Tag { get; set; }                // grammar, tense, topic...
        public string? TagType { get; set; } // Thêm để phân loại (e.g., "grammar", "vocabulary", "topic" – business: filter advanced)
    }
}
