using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs.Questions
{
    // 1. Enum loại câu hỏi (để Frontend biết render radio hay checkbox)
    public enum QuestionType
    {
        SingleChoice = 1,   // Chọn 1 (TOEIC đa số dùng cái này)
        MultipleChoice = 2, // Chọn nhiều (IELTS hay có)
        FillBlank = 3,      // Điền từ (Part 6 TOEIC nếu muốn làm dạng input)
        Matching = 4        // Nối
    }
}
