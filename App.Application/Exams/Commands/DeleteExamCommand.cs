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
    // ============================================
    // UC-XX.1: XÓA ĐỀ THI (SOFT DELETE)
    // ============================================
    public record DeleteExamCommand : IRequest<bool>
    {
        public Guid ExamId { get; set; }
        public bool HardDelete { get; set; } = false; // Tùy chọn: xóa cứng hay mềm
    }

    public class DeleteExamHandler : IRequestHandler<DeleteExamCommand, bool>
    {
        private readonly IAppDbContext _context;

        public DeleteExamHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteExamCommand request, CancellationToken cancellationToken)
        {
            // === VALIDATION ===
            if (request.ExamId == Guid.Empty)
                throw new ValidationException("ExamId không hợp lệ");

            // === GET EXAM ===
            var exam = await _context.Exams
                .FirstOrDefaultAsync(x => x.Id == request.ExamId, cancellationToken);

            if (exam == null)
                throw new Exception("Đề thi không tồn tại");

            // === BUSINESS RULE: KIỂM TRA ĐÃ CÓ HỌC VIÊN LÀM BÀI CHƯA ===
            var hasResults = await _context.ExamResults
                .AnyAsync(er => er.ExamId == request.ExamId, cancellationToken);

            if (hasResults)
                throw new Exception("Không thể xóa đề thi đã có học viên làm bài. Hãy chuyển sang trạng thái Archived thay vì xóa.");

            // === XÓA THEO LOẠI ===
            if (request.HardDelete)
            {
                // HARD DELETE: Xóa hẳn khỏi DB (nguy hiểm)
                // Xóa cascade: ExamQuestions → ExamSections → Exam
                var sections = await _context.ExamSections
                    .Include(s => s.ExamQuestions)
                    .Where(s => s.ExamId == request.ExamId)
                    .ToListAsync(cancellationToken);

                foreach (var section in sections)
                {
                    _context.ExamQuestions.RemoveRange(section.ExamQuestions);
                }

                _context.ExamSections.RemoveRange(sections);
                _context.Exams.Remove(exam);
            }
            else
            {
                // SOFT DELETE: Chỉ đánh dấu IsActive = false
                exam.IsActive = false;
                exam.Status = ExamStatus.Archived;
                exam.UpdatedAt = DateTime.UtcNow;
            }

            // === SAVE ===
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }

}
