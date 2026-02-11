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

            // === BUSINESS RULE: CHỈ SỬA ĐƯỢC KHI DRAFT ===
            if (exam.Status != ExamStatus.Draft)
                throw new Exception("Chỉ được sửa đề thi ở trạng thái Draft");

            // === CHECK DUPLICATE CODE ===
            if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != exam.Code)
            {
                var codeExists = await _context.Exams
                    .AnyAsync(e => e.Code == request.Code && e.IsActive && e.Id != request.ExamId, cancellationToken);

                if (codeExists)
                    throw new ValidationException($"Mã đề thi '{request.Code}' đã tồn tại");

                exam.Code = request.Code;
            }

            // === UPDATE FIELDS (Chỉ update field không null) ===
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                if (request.Title.Length > 200)
                    throw new ValidationException("Tên đề thi tối đa 200 ký tự");
                exam.Title = request.Title;
            }

            if (request.Description != null)
                exam.Description = request.Description;

            if (request.Duration.HasValue)
            {
                if (request.Duration.Value <= 0)
                    throw new ValidationException("Thời gian thi phải lớn hơn 0");
                if (request.Duration.Value > 300)
                    throw new ValidationException("Thời gian thi tối đa 300 phút");
                exam.Duration = request.Duration.Value;
            }

            if (request.Type.HasValue)
                exam.Type = request.Type.Value;

            if (request.ShuffleQuestions.HasValue)
                exam.ShuffleQuestions = request.ShuffleQuestions.Value;

            if (request.ShuffleAnswers.HasValue)
                exam.ShuffleAnswers = request.ShuffleAnswers.Value;

            // Update version để track changes
            exam.Version++;
            exam.UpdatedAt = DateTime.UtcNow;

            // === SAVE ===
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
