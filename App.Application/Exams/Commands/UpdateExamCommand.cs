using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Exams.Commands
{
    // CẬP NHẬT THÔNG TIN ĐỀ THI
    public record UpdateExamCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public ExamType? Type { get; set; }
        public bool? ShuffleQuestions { get; set; }
        public bool? ShuffleAnswers { get; set; }

        // 👇 THÊM TRƯỜNG NÀY ĐỂ HỨNG DATA "status: 0" TỪ FRONTEND
        public ExamStatus? Status { get; set; }
    }

    public class UpdateExamHandler : IRequestHandler<UpdateExamCommand, bool>
    {
        private readonly IAppDbContext _context;

        public UpdateExamHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateExamCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            // === GET EXAM ===
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == request.ExamId, cancellationToken);

            if (exam == null)
                throw new Exception("Đề thi không tồn tại");

            if (!exam.IsActive)
                throw new Exception("Đề thi đã bị xóa");

            // === CHECK DUPLICATE CODE ===
            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != exam.Code)
            {
                var codeExists = await _context.Exams
                    .AnyAsync(e => e.Code == request.Code && e.IsActive && e.Id != request.ExamId, cancellationToken);

                if (codeExists)
                    throw new ValidationException($"Mã đề thi '{request.Code}' đã tồn tại");

                exam.Code = request.Code;
            }

            bool isModified = false;

            // === UPDATE FIELDS (Chỉ update field không null) ===
            if (!string.IsNullOrWhiteSpace(request.Title) && request.Title != exam.Title)
            {
                if (request.Title.Length > 200)
                    throw new ValidationException("Tên đề thi tối đa 200 ký tự");
                exam.Title = request.Title;
                isModified = true;
            }

            if (request.Description != null && request.Description != exam.Description)
            {
                exam.Description = request.Description;
                isModified = true;
            }

            if (request.Duration.HasValue && request.Duration.Value != exam.Duration)
            {
                if (request.Duration.Value <= 0)
                    throw new ValidationException("Thời gian thi phải lớn hơn 0");
                if (request.Duration.Value > 300)
                    throw new ValidationException("Thời gian thi tối đa 300 phút");
                exam.Duration = request.Duration.Value;
                isModified = true;
            }

            if (request.Type.HasValue && request.Type.Value != exam.Type)
            {
                exam.Type = request.Type.Value;
                isModified = true;
            }

            if (request.ShuffleQuestions.HasValue && request.ShuffleQuestions.Value != exam.ShuffleQuestions)
            {
                exam.ShuffleQuestions = request.ShuffleQuestions.Value;
                isModified = true;
            }

            if (request.ShuffleAnswers.HasValue && request.ShuffleAnswers.Value != exam.ShuffleAnswers)
            {
                exam.ShuffleAnswers = request.ShuffleAnswers.Value;
                isModified = true;
            }

            if (request.Status.HasValue && request.Status.Value != exam.Status)
            {
                exam.Status = request.Status.Value;
                isModified = true;
            }

            if (isModified || (request.Code != null && request.Code != exam.Code))
            {
                // Update version để rack changes
                exam.Version++;
                exam.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
    }
}