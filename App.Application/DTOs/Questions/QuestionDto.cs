using App.Domain.Entities;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Application.DTOs.Questions
{
    public class QuestionListDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public Guid? GroupId { get; set; }   // Có thuộc bài đọc nào không?
        public string? Content { get; set; }  // HTML nội dung câu hỏi
        public string? ThumbnailUrl { get; set; }
        public bool HasAudio { get; set; }
        public string QuestionType { get; set; } // "SingleChoice", "Essay", "FillBlank"
                                                 // --- 3. ĐỘ KHÓ (THEO LOGIC MỚI) ---
        public Guid? DifficultyId { get; set; }
        public string? DifficultyName { get; set; } // Hiển thị: "Hard", "Band 8.0"
        public string? DifficultyCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreateAt { get; set; }
        public int AnswerCount { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class QuestionGroupListDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }

        public int QuestionCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<MediaDto> Media { get; set; } = [];
    }

    public class CreateQuestionDto
    {
        // --- CƠ BẢN ---
        public Guid CategoryId { get; set; } // Part mấy? Topic gì?
        public Guid? GroupId { get; set; }   // Có thuộc bài đọc nào không?
        public string? Content { get; set; }  // HTML nội dung câu hỏi
        public string QuestionType { get; set; } // "SingleChoice", "Essay", "FillBlank"
        public int DifficultyLevel { get; set; } = 1; // 1-Easy, 2-Medium, 3-Hard
        public bool IsActive { get; set; } = true;
        public string? Explanation { get; set; }

        // --- CẤU HÌNH (SETTINGS) ---
        public bool ShuffleAnswers { get; set; } = true; // Trắc nghiệm có đảo đáp án ko?
        public double DefaultScore { get; set; } = 1.0;

        // --- DÀNH RIÊNG CHO WRITING/SPEAKING (IELTS) ---
        public string? PromptType { get; set; } // Vd: "Argumentative", "Chart Description"
        public int? MinWords { get; set; }
        public int? MaxWords { get; set; }
        public string? RubricJson { get; set; } // Tiêu chí chấm điểm (JSON)

        // --- LIST CON ---
        public List<CreateAnswerDto> Answers { get; set; } = new();
        public List<CreateMediaDto> Media { get; set; } = new();
        public List<string> Tags { get; set; } = new(); // List tên tag: "Grammar", "Tenses"
    }
    public class SingleQuestionDetailDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }

        public string QuestionType { get; set; } = string.Empty;
        public Guid? DifficultyId { get; set; }
        public string? DifficultyName { get; set; } // Hiển thị: "Hard", "Band 8.0"
        public string? DifficultyCode { get; set; }
        public double DefaultScore { get; set; }
        public bool ShuffleAnswers { get; set; }
        public bool IsActive { get; set; }

        // Nội dung
        public string Content { get; set; } = string.Empty;
        public string? Explanation { get; set; }

        // Media của câu hỏi (audio / image)
        public List<MediaDto> Media { get; set; } = [];

        // 4 đáp án
        public List<AnswerDto> Answers { get; set; } = [];
    }

    public class QuestionGroupDetailDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }

        // Nội dung group
        public string? Content { get; set; }          // Passage / hội thoại
        public string? Explanation { get; set; }
        public string? Transcript { get; set; }
        public Guid? DifficultyId { get; set; }
        public string? DifficultyName { get; set; } // Hiển thị: "Hard", "Band 8.0"
        public string? DifficultyCode { get; set; }
        public string? MediaJson { get; set; }
        public bool IsActive { get; set; }

        // Media của group (audio / image)
        public List<MediaDto> Media { get; set; } = [];

        // Danh sách câu hỏi con
        public List<GroupQuestionItemDto> Questions { get; set; } = [];
    }
    public class GroupQuestionItemDto
    {
        public Guid Id { get; set; }

        public string QuestionType { get; set; } = string.Empty;
        public Guid? DifficultyId { get; set; }
        public double DefaultScore { get; set; }

        public string Content { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public List<MediaDto> Media { get; set; } = [];
        public List<AnswerDto> Answers { get; set; } = [];
    }


    public class CreateAnswerDto
    {
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; } // Giải thích tại sao đúng/sai
        public int OrderIndex { get; set; }
        public string? AudioUrl { get; set; }
    }

    public class CreateMediaDto
    {
        public Guid? Id { get; set; }
        public string Url { get; set; }
        public string PublicId { get; set; }
        public string MediaType { get; set; }
        public int OrderIndex { get; set; } = 1; // thêm
    }

    public class MediaDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty; // Image | Audio
        public int OrderIndex { get; set; }
    }
    public class AnswerDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string? Feedback { get; set; }
        public int OrderIndex { get; set; }

        public List<MediaDto> Media { get; set; } = [];
    }


    public class QuestionProfile : Profile
    {
        public QuestionProfile()
        {
            // =========================================================
            // 1. CHIỀU WRITE (CreateQuestionDto -> Entity)
            // =========================================================
            CreateMap<CreateQuestionDto, Question>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())        // ID tự sinh
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Tự set lúc new
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())

                // Các Collection này thường xử lý thủ công trong Handler để gán ID cha
                // Nhưng nếu muốn AutoMapper làm luôn thì bỏ Ignore đi
                .ForMember(dest => dest.Tags, opt => opt.Ignore())
                .ForMember(dest => dest.Media, opt => opt.Ignore())
                .ForMember(dest => dest.Answers, opt => opt.Ignore());

            // Map con: Answer
            CreateMap<CreateAnswerDto, Answer>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionId, opt => opt.Ignore()); // Sẽ gán sau khi tạo Question

            // Map con: Media
            CreateMap<CreateMediaDto, QuestionMedia>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionId, opt => opt.Ignore());

            // =========================================================
            // 2. CHIỀU READ (Entity -> QuestionListDto)
            // 👉 QUAN TRỌNG: Dùng cho màn hình danh sách (Master)
            // =========================================================
            CreateMap<Question, QuestionListDto>()
                // 1. Map tên danh mục (Flattening)
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.DifficultyName, opt => opt.MapFrom(src => src.Difficulty != null ? src.Difficulty.Name : null))
                .ForMember(dest => dest.DifficultyCode, opt => opt.MapFrom(src => src.Difficulty != null ? src.Difficulty.Code : null))
                .ForMember(dest => dest.ThumbnailUrl,
                            opt => opt.MapFrom(src =>
                                src.Media
                                   .Where(m => m.MediaType == "Image")
                                   .OrderBy(m => m.OrderIndex)
                                   .Select(m => m.Url)
                                   .FirstOrDefault()
                            ))
                        .ForMember(dest => dest.HasAudio,
                            opt => opt.MapFrom(src =>
                                src.Media.Any(m => m.MediaType == "Audio")
                            ))
                // 2. Đếm số lượng đáp án (Thay vì load cả list)
                // Lưu ý: Nếu có Soft Delete thì nhớ filter (!IsDeleted)
                .ForMember(dest => dest.AnswerCount, opt => opt.MapFrom(src => src.Answers != null ? src.Answers.Count : 0))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags.Select(t => t.Tag).ToList()))
                // 3. Map lệch tên (DTO: CreateAt - Entity: CreatedAt)
                .ForMember(dest => dest.CreateAt, opt => opt.MapFrom(src => src.CreatedAt));


            CreateMap<QuestionGroup, QuestionGroupListDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty));

            CreateMap<Question, PracticeQuestionDto>()
                .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.Id)) // <--- QUAN TRỌNG: Map Id sang QuestionId
                .ForMember(dest => dest.QuestionNumber, opt => opt.Ignore()) // Cái này tính toán trong vòng lặp, ko map từ DB
                .ForMember(dest => dest.OrderIndex, opt => opt.Ignore())
                // Map các object con
                .ForMember(dest => dest.Answers, opt => opt.MapFrom(src => src.Answers))
                .ForMember(dest => dest.Media, opt => opt.MapFrom(src => src.Media));

            // Map cho Media trong màn Practice
            CreateMap<QuestionMedia, PracticeMediaDto>(); // Đảm bảo bạn đã có class PracticeMediaDto
                                                          // Bỏ comment và sửa lại phần này ở cuối file QuestionProfile
            CreateMap<Question, SingleQuestionDetailDto>()
                .ForMember(dest => dest.DifficultyName, opt => opt.MapFrom(src => src.Difficulty != null ? src.Difficulty.Name : null))
                .ForMember(dest => dest.DifficultyCode, opt => opt.MapFrom(src => src.Difficulty != null ? src.Difficulty.Code : null));

            CreateMap<Answer, AnswerDto>(); // Map đáp án chi tiết
            CreateMap<QuestionMedia, MediaDto>(); // Map media chi tiết
        }
    }
}
