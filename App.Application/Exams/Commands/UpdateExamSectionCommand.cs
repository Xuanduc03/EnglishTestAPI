using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace App.Application.Exams.Commands
{
    // Cập nhật section phần thi
    public record UpdateExamSectionCommand : IRequest<bool>
    {
        public Guid SectionId { get; set; }
        public Guid? CategoryId { get; set; }
        public string? Instructions { get; set; }
        public int? OrderIndex { get; set; }
        public int? TimeLimit { get; set; }
    }

    public class UpdateExamSectionHandler : IRequestHandler<UpdateExamSectionCommand, bool>
    {
        private readonly IAppDbContext _context;

        public UpdateExamSectionHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(UpdateExamSectionCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.SectionId == Guid.Empty)
                throw new ValidationException("SectionId không hợp lệ");

            // === GET SECTION ===
            var section = await _context.ExamSections
                .Include(s => s.Exam)
                .FirstOrDefaultAsync(s => s.Id == request.SectionId, cancellationToken);

            if (section == null)
                throw new Exception("Chỉ được sửa đề thi ở trạng thái Draft");

            // === CHECK EXAM STATUS ===
            if (section.Exam.Status != ExamStatus.Draft)
                throw new Exception("Chỉ được sửa đề thi ở trạng thái Draft");

            // === UPDATE CATEGORY ===
            if (request.CategoryId.HasValue)
            {
                var newCategoryId = request.CategoryId.Value;

                var categoryExists = await _context.Categories
                    .AnyAsync(x => x.Id == newCategoryId, cancellationToken);

                if (!categoryExists)
                    throw new ValidationException("Category không tồn tại");

                // 🔥 CHECK DUPLICATE CATEGORY IN EXAM
                if (newCategoryId != section.CategoryId)
                {
                    var duplicate = await _context.ExamSections
                        .AnyAsync(x =>
                            x.ExamId == section.ExamId &&
                            x.CategoryId == newCategoryId &&
                            x.Id != section.Id &&
                            !x.IsDeleted,
                            cancellationToken);

                    if (duplicate)
                        throw new ValidationException("Category này đã tồn tại trong đề thi");
                }

                section.CategoryId = newCategoryId;
            }


            if (request.Instructions != null)
                section.Instructions = request.Instructions;

            if (request.OrderIndex.HasValue)
            {
                if (request.OrderIndex.Value < 0)
                    throw new ValidationException("Thứ tự phải >= 0");
                section.OrderIndex = request.OrderIndex.Value;
            }

            if (request.TimeLimit.HasValue)
            {
                if (request.TimeLimit.Value <= 0)
                    throw new ValidationException("Thời gian phải > 0");
                section.TimeLimit = request.TimeLimit.Value;
            }

            section.UpdatedAt = DateTime.UtcNow;

            // === SAVE ===
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

}
