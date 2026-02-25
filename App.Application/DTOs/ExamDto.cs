// DTO quản lý đề thi

using App.Domain.Entities;
using AutoMapper;
using System.ComponentModel.DataAnnotations;

namespace App.Application.DTOs
{
    // Các trường cơ bản của Exam
    public abstract class ExamBaseDto
    {
        public string Code { get; set; } // ETS-2024-01
        public string Title { get; set; }
        public string? Description { get; set; }
        public int Duration { get; set; } // Phút
        public decimal? Price { get; set; }
    }

    // DTO nhẹ: Dùng cho màn hình "Danh sách đề thi" (Table)
    public class ExamSummaryDto : ExamBaseDto
    {
        public Guid Id { get; set; }
        public decimal TotalScore { get; set; }
        public string Status { get; set; }      // "Draft", "Published"
        public int QuestionCount { get; set; }  // Tổng số câu hỏi
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO nặng: Dùng cho màn hình "Cấu trúc đề thi" (Chi tiết)
    // Chứa cả Sections và Questions bên trong
    public class ExamDetailDto : ExamSummaryDto
    {
        // Danh sách các phần thi (Listening, Reading...)
        public List<ExamSectionDto> Sections { get; set; } = new();
    }

    // DTO cho từng Phần thi (Section)
    public class ExamSectionDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } 
        public string Name { get; set; }
        public string? Instructions { get; set; }
        public int OrderIndex { get; set; }

        // Danh sách câu hỏi trong phần này
        public List<ExamQuestionDto> Questions { get; set; } = new();
    }

    // DTO cho từng Câu hỏi trong đề (Mapping)
    public class ExamQuestionDto
    {
        public Guid Id { get; set; }           // ID của ExamQuestion (Bảng trung gian)
        public Guid QuestionId { get; set; }   // ID câu hỏi gốc trong kho

        // Thông tin hiển thị sơ lược để Admin nhận diện câu hỏi
        public string ContentPreview { get; set; }
        public string QuestionType { get; set; }
        public string DifficultyName { get; set; }

        public decimal Point { get; set; }      // Điểm số của câu này trong đề
        public int OrderIndex { get; set; }    // Thứ tự câu (1, 2, 3...)
    }

    #region CRUD Exam DTO
    // [UC-22] Payload tạo đề thi THỦ CÔNG (Đơn giản nhất)
    public class CreateExamDto : ExamBaseDto
    {
        // Mặc định tạo là Draft
        public string Status { get; set; } = "Draft";
    }

    // Dto trả về kết quả 
    public class ExamCreatedDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }
        public ExamStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }


    // Payload cập nhật thông tin chung (Rename, đổi giờ...)
    public class UpdateExamDto : ExamBaseDto
    {
        public string Status { get; set; }
    }

    // [UC-21] Payload tạo đề TỰ ĐỘNG (Sinh từ ma trận)
    public class GenerateExamDto : ExamBaseDto
    {
        [Required]
        public Guid MatrixId { get; set; } // ID ma trận cấu hình
        public int? RandomSeed { get; set; } // Tùy chọn để test
    }
    #endregion


    // Thêm câu hỏi vào đề (Add Question to Exam)
    public class AddQuestionToExamDto
    {
        [Required]
        public Guid ExamId { get; set; }

        public Guid? SectionId { get; set; } // Nếu đề chia section thì bắt buộc

        [Required]
        public List<Guid> QuestionIds { get; set; } // Chọn 1 lúc nhiều câu

        public decimal DefaultPoint { get; set; } = 5.0m; // Điểm mặc định
    }

    // Cập nhật vị trí/điểm số của câu hỏi trong đề
    public class UpdateExamQuestionOrderDto
    {
        public Guid ExamQuestionId { get; set; } // ID bảng trung gian
        public int NewOrderIndex { get; set; }
        public decimal? NewPoint { get; set; }
    }

    // Tạo Section mới cho đề (Ví dụ: Thêm phần "Speaking")
    public class CreateExamSectionDto
    {
        public Guid ExamId { get; set; }
        public string Name { get; set; }
        public string? Instructions { get; set; }
        public int OrderIndex { get; set; }
    }

    #region DTO Preview exam query
    // ── Response: cùng "shape" với StartExamResult, thêm IsCorrect ──
    public class ExamPreviewDto
    {
        // Giả lập attempt (KHÔNG phải record thật)
        public Guid AttemptId { get; set; } = Guid.Empty;  // sentinel: đây là preview
        public bool IsPreview { get; set; } = true;
        public bool ShowCorrectAnswers { get; set; }

        // Metadata exam
        public Guid ExamId { get; set; }
        public string ExamTitle { get; set; }
        public string ExamCode { get; set; }
        public string Status { get; set; }             // Draft / Published
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Giống hệt StartExamResult
        public DateTime StartedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int TotalQuestions { get; set; }
        public List<PreviewSectionDto> Sections { get; set; } = new();
    }

    public class PreviewSectionDto
    {
        public Guid SectionId { get; set; }
        public string SectionName { get; set; }
        public string SkillType { get; set; }
        public int OrderIndex { get; set; }
        public string? Instructions { get; set; }
        public List<PreviewQuestionDto> Questions { get; set; } = new();
    }

    public class PreviewQuestionDto
    {
        // Giống ExamQuestionPreview của Student
        public Guid ExamQuestionId { get; set; }
        public Guid QuestionId { get; set; }
        public int OrderIndex { get; set; }
        public double Point { get; set; }
        public string Content { get; set; }
        public string QuestionType { get; set; }
        public string? AudioUrl { get; set; }
        public string? ImageUrl { get; set; }
        public string? Explanation { get; set; }
        public string? ExplanationVi { get; set; }

        // ✅ Preview thêm IsCorrect — Student KHÔNG có field này
        public List<PreviewAnswerOption> Answers { get; set; } = new();
    }

    public class PreviewAnswerOption
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public int OrderIndex { get; set; }
        public bool IsCorrect { get; set; }  // ✅ Gửi cho Admin
    }
    #endregion
    public class ExamProfile : Profile
    {
        public ExamProfile()
        {
            // 1. Map Exam -> Summary
            CreateMap<Exam, ExamSummaryDto>()
                .ForMember(d => d.QuestionCount, opt => opt.MapFrom(s => s.Sections.SelectMany(x => x.ExamQuestions).Count()));

            // 2. Map Exam -> Detail (Bao gồm cả Sections)
            CreateMap<Exam, ExamDetailDto>();

            // 3. Map Section 
            CreateMap<ExamSection, ExamSectionDto>()
                // Map danh sách câu hỏi và sắp xếp luôn
                .ForMember(d => d.Questions, opt => opt.MapFrom(s => s.ExamQuestions.OrderBy(q => q.OrderIndex)))
                // Map tên từ Category
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Category.Name));

            // 4. Map ExamQuestion 
            CreateMap<ExamQuestion, ExamQuestionDto>()
                .ForMember(d => d.ContentPreview, opt => opt.MapFrom(s => s.Question.Content))
                .ForMember(d => d.QuestionType, opt => opt.MapFrom(s => s.Question.QuestionType))
                .ForMember(d => d.DifficultyName, opt => opt.MapFrom(s => s.Question.Difficulty.Name));

            // 5. Map Create -> Entity
            CreateMap<CreateExamDto, Exam>();
            CreateMap<UpdateExamDto, Exam>();
        }
    }
}
