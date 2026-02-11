using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
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
