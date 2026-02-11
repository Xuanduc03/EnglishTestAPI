using App.Application.DTOs;
using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;


namespace App.Application.Exams.Commands
{
    public record CreateExamCommand : IRequest<Guid>
    {
        public string Code { get; init; }
        public string Title { get; init; }
        public string? Description { get; init; } 
        public int Duration { get; init; }
        public decimal TotalScore { get; init; }
        // Phân loại (BỔ SUNG MỚI)
        public ExamType Type { get; init; }                      // TOEIC, IELTS...
        public ExamCategory Category { get; init; }              // FullTest, PartTest, SkillTest...
        public ExamScope Scope { get; init; }                    // Full, Part5Only, ListeningOnly...
        public ExamLevel Level { get; init; } = ExamLevel.Practice; // Practice, MockTest...

        // Tags & Metadata (OPTIONAL)
        public List<string>? Tags { get; init; }                 // ["Grammar", "Part 5"]
        public Dictionary<string, object>? MetaData { get; init; } // Flexible metadata

        // Cài đặt
        public bool ShuffleQuestions { get; init; } = false;
        public bool ShuffleAnswers { get; init; } = false;
        public bool IsActive { get; init; } = true;

    }


    public class CreateExamManualCommandHandler : IRequestHandler<CreateExamCommand, Guid>
    {
        private readonly IAppDbContext _context;

        public CreateExamManualCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateExamCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (string.IsNullOrWhiteSpace(request.Code))
                throw new ValidationException("Mã đề thi không được để trống");

            if (request.Code.Length > 50)
                throw new ValidationException("Mã đề thi tối đa 50 ký tự");

            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ValidationException("Tên đề thi không được để trống");

            if (request.Title.Length > 200)
                throw new ValidationException("Tên đề thi tối đa 200 ký tự");

            if (request.Duration <= 0)
                throw new ValidationException("Thời gian thi phải lớn hơn 0");

            if (request.Duration > 300)
                throw new ValidationException("Thời gian thi tối đa 300 phút");

            // Check trùng mã đề
            var existingExam = await _context.Exams.AnyAsync(x => x.Code == request.Code && x.IsActive, cancellationToken);
            if (existingExam)
                throw new ValidationException($"Mã đề thi '{request.Code}' đã tồn tại");


            // 7. Create exam
            var exam = new Exam
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Title = request.Title,
                Description = request.Description,
                Duration = request.Duration,
                TotalScore = request.TotalScore,

                // Phân loại
                Type = request.Type,
                Category = request.Category,
                Scope = request.Scope,
                Level = request.Level,

                // Tags & Metadata
                Tags = request.Tags != null ? System.Text.Json.JsonSerializer.Serialize(request.Tags) : null,
                MetaData = request.MetaData != null ? System.Text.Json.JsonSerializer.Serialize(request.MetaData) : null,

                // Cài đặt
                ShuffleQuestions = request.ShuffleQuestions,
                ShuffleAnswers = request.ShuffleAnswers,
                Status = request.IsActive ? ExamStatus.Published : ExamStatus.Draft,
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Exams.Add(exam);


            await _context.SaveChangesAsync(cancellationToken);
            return exam.Id;
        }
    }
}
