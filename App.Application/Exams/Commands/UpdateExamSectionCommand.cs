using App.Application.Interfaces;
using App.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // === UPDATE FIELDS ===
            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(x => x.Id == request.CategoryId);

                if (!categoryExists)
                    throw new ValidationException("Category không tồn tại");

                section.CategoryId = request.CategoryId.Value;
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
