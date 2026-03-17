using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Entities
{
    public class ScoreTableEntry : BaseEntity
    {
        // Thuộc bảng quy đổi nào
        public Guid ScoreTableId { get; set; }
        public virtual ScoreTable ScoreTable { get; set; }

        // Số câu đúng: 0 → 100
        public int CorrectAnswers { get; set; }

        // Điểm tương ứng: 5 → 495
        public int Score { get; set; }
    }
}
