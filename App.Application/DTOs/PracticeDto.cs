using App.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs
{
    // ==================== REQUEST ====================

    // 1. Request tạo practice
    public class CreatePracticeRequest
    {
        public List<Guid> PartIds { get; set; } = new(); // Part 1,2,3,4,5,6,7
        public int QuestionsPerPart { get; set; } = 10; // Số câu mỗi part
        public bool IsTimed { get; set; }
        public int? TimeLimitMinutes { get; set; }
    }

    // ==================== RESPONSE ====================

    // 2. Practice session response
    public class PracticeSessionDto
    {
        public Guid SessionId { get; set; } // ExamId
        public string Title { get; set; }
        public int TotalQuestions { get; set; }
        public int Duration { get; set; } // phút
        public List<PracticePartDto> Parts { get; set; } = new();
    }

    // 3. Mỗi Part (Part 1-7)
    public class PracticePartDto
    {
        public Guid PartId { get; set; } // CategoryId
        public string PartName { get; set; } // "Part 1", "Part 2"...
        public int PartNumber { get; set; } // ✅ THÊM: 1-7 để dễ identify
        public string? PartDescription { get; set; } // ✅ THÊM: "Photographs", "Conversations"...
        public List<PracticeQuestionDto> Questions { get; set; } = new();
    }

    // 4. Câu hỏi (dùng chung cho cả single và group)
    public class PracticeQuestionDto
    {
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; } // Thứ tự trong part: 1,2,3...
        public int QuestionNumber { get; set; } // ✅ THÊM: Số câu tổng thể 1-200

        // ==================== GROUP QUESTION INFO ====================
        // Nếu là group question (Part 3,4,6,7)
        public Guid? GroupId { get; set; }
        public string? GroupContent { get; set; } // Passage/Conversation cho Part 6,7
        public string? Explanation { get; set; }
        public List<PracticeMediaDto>? GroupMedia { get; set; } // Audio cho Part 3,4
        public bool HasAudio { get; set; }
        public bool HasImage { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }

        // ✅ THÊM: Group metadata
        public int? TotalQuestionsInGroup { get; set; } // 3 cho P3/P4, 4 cho P6, 2-5 cho P7
        public int? QuestionIndexInGroup { get; set; } // Vị trí: 1/3, 2/3, 3/3

        // ✅ THÊM: Multiple passages support (Part 7 Double/Triple)
        public List<GroupPassageDto>? Passages { get; set; }

        // ==================== QUESTION CONTENT ====================
        public string Content { get; set; }
        public List<PracticeMediaDto> Media { get; set; } = new(); // Image/Audio cho Part 1,2

        // ==================== ANSWERS ====================
        public List<PracticeAnswerDto> Answers { get; set; } = new();

        // ==================== USER STATE ====================
        public Guid? SelectedAnswerId { get; set; }
        public bool? IsCorrect { get; set; }
        public bool IsMarkedForReview { get; set; } // ✅ THÊM: Đánh dấu để review
    }

    // ✅ THÊM: Support cho Part 7 Multiple Passages
    public class GroupPassageDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; } // 1, 2, 3 for triple passage
        public string? Title { get; set; } // "Email", "Article", "Advertisement"
        public string? PassageType { get; set; } // "email", "article", "notice", "chat"
    }

    // 5. Media (Hình ảnh/Audio) - DÙNG CHUNG
    public class PracticeMediaDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string? Description { get; set; }
    }

    // 6. Đáp án
    public class PracticeAnswerDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; } // A=0, B=1, C=2, D=3
        public bool IsCorrect { get; set; }
        public List<PracticeMediaDto> Media { get; set; } = new(); // Nếu answer có audio (Part 2)
    }

    // ==================== SUBMIT & RESULT ====================

    public class SubmitPracticeAnswerRequest
    {
        public Guid SessionId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid? AnswerId { get; set; }
        public bool? IsMarkedForReview { get; set; } // ✅ THÊM
    }

    public class PracticeResultDto
    {
        public Guid SessionId { get; set; }
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public int UnansweredQuestions { get; set; } // ✅ THÊM
        public double Score { get; set; }
        public double AccuracyPercentage { get; set; } // ✅ THÊM
        public TimeSpan TotalTime { get; set; } // ✅ THÊM
        public Dictionary<string, PartResultDto> PartResults { get; set; } = new();
    }

    public class PartResultDto
    {
        public string PartName { get; set; }
        public int PartNumber { get; set; } // ✅ THÊM
        public int Total { get; set; }
        public int Correct { get; set; }
        public int Incorrect { get; set; } // ✅ THÊM
        public int Unanswered { get; set; } // ✅ THÊM
        public double Percentage { get; set; }
        public double AverageTimePerQuestion { get; set; } // ✅ THÊM: seconds
    }

    // ==================== DTO trạng thái của practice đang làm ====================
  
    public class InProgressPracticeDto
    {
        public Guid AttemptId { get; set; }
        public string Title { get; set; }
        public string CategoryName { get; set; } // Tên category (Part 3 & 4, v.v.)
        public double Progress { get; set; }      // % hoàn thành
        public DateTime StartedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public int? TimeLimitSeconds { get; set; }
        public int? ActualTimeSeconds { get; set; }
    }
}