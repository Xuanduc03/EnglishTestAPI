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
    // Xóa phần thi trong đề thi
    public record DeleteExamSectionCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public Guid SectionId { get; set; }
    }

    public class DeleteExamSectionHandler : IRequestHandler<DeleteExamSectionCommand, bool>
    {
        private readonly IAppDbContext _context;

        public DeleteExamSectionHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteExamSectionCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            if (request.SectionId == Guid.Empty)
                throw new ValidationException("SectionId không hợp lệ");

            // === GET SECTION WITH QUESTIONS ===
            var section = await _context.ExamSections
                .Include(s => s.Exam)
                .Include(s => s.ExamQuestions)
                .FirstOrDefaultAsync(s => s.Id == request.SectionId, cancellationToken);

            if (section == null)
                throw new Exception("Section không tồn tại");

            if (section.ExamId != request.ExamId)
                throw new Exception("Section không thuộc đề thi này");

            // === CHECK EXAM STATUS ===
            if (section.Exam.Status != ExamStatus.Draft)
                throw new Exception("Chỉ được sửa đề thi ở trạng thái Draft");

            // === XÓA TẤT CẢ EXAM QUESTIONS TRONG SECTION ===
            if (section.ExamQuestions.Any())
            {
                _context.ExamQuestions.RemoveRange(section.ExamQuestions);
            }

            // === XÓA SECTION ===
            _context.ExamSections.Remove(section);

            // === CẬP NHẬT TOTAL SCORE CỦA EXAM ===
            var newTotalScore = await _context.ExamQuestions
                .Where(eq => eq.ExamId == request.ExamId)
                .SumAsync(eq => eq.Point, cancellationToken);

            section.Exam.TotalScore = newTotalScore;

            // === REORDER CÁC SECTION CÒN LẠI ===
            var remainingSections = await _context.ExamSections
                .Where(s => s.ExamId == request.ExamId && s.OrderIndex > section.OrderIndex)
                .ToListAsync(cancellationToken);

            foreach (var s in remainingSections)
            {
                s.OrderIndex--;
            }

            // === SAVE ===
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
